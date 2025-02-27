using DynamicBlockRelocationDemo.BlockRelocation.DynamicImprovements;
using DynamicBlockRelocationDemo.BlockRelocation.Model;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Events;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Generators;
using DynamicBlockRelocationDemo.BlockRelocation.Simulator.Stats;
using DynamicBlockRelocationDemo.BlockRelocation.TreeSearch;
using DynamicBlockRelocationDemo.DynamicMethods;
using DynamicBlockRelocationDemo.DynamicSolvers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TreesearchLib;

namespace DynamicBlockRelocationDemo.BlockRelocation.Simulator
{
	public class BlockRelocationSimulator
    {
        public List<Move> ExecutedMoves { get; set; } = new List<Move>(); //includes all moves that were executed
        public Queue<Move> PlannedMoves { get; set; }

        private readonly EventGenerator _eventGenerator;
        private BlockRelocationProblemState? previousState;
        private BlockRelocationProblemState _currentState;
        private BlockRelocationProblemState _targetSolutionState;

        private SimulationStats _simulationStats;
        private ICostCalculator _costCalculator;
        public readonly SimulationConfig _config;
        private readonly Stopwatch _simulationStopwatch;
        private readonly BetterStartingPointFinder restartPointFinder;
        private int _processedEvents = 0;

		private readonly IDynamicUpdateStrategy _recalculationStrategy;


		public BlockRelocationSimulator(BlockRelocationProblemState initialState, BlockRelocationProblemState solutionState, SimulationConfig config)
        {
            _currentState = initialState;
            _targetSolutionState = solutionState ?? CalculateInitialSolution(initialState, config);
            PlannedMoves = new Queue<Move>(_targetSolutionState.AppliedMoves.Reverse());
            _eventGenerator = new EventGenerator(config.EventProbabilities);
            _config = config;
            _simulationStats = new SimulationStats(config.TotalEvents);
            _costCalculator = config.CostCalculator;
            _simulationStopwatch = new Stopwatch();
            restartPointFinder = new BetterStartingPointFinder(_costCalculator);
			var strategyFactory = new DynamicRecalculationStrategyFactory(_costCalculator, config);
			_recalculationStrategy = strategyFactory.CreateStrategy(config.DynamicVariant);
		}
        private BlockRelocationProblemState CalculateInitialSolution(BlockRelocationProblemState initialState, SimulationConfig config)
        {
            var solution = initialState.BeamSearch(10, state => state.Bound.Value, runtime: TimeSpan.FromSeconds(1000));
            if (solution == null)
                throw new InvalidOperationException("Could not find initial solution");
            return solution;
        }

        public SimulationResult Run()
        {
            _simulationStopwatch.Start();
            Console.WriteLine("Simulator started");
            try
            {
                while (ShouldContinueSimulation())
                {
                    Console.Write($"Event {_processedEvents + 1}/{_config.TotalEvents}, Moves to go: {PlannedMoves.Count}, Next Event: ");
                    var nextEvent = _eventGenerator.GenerateRandomEvent(this);
                    LogEventType(nextEvent);
                    HandleEvent(nextEvent);
                    _processedEvents++;
                }
                _simulationStopwatch.Stop();
                Console.WriteLine("Simulator completed");
                Console.WriteLine($"Total simulation time: {_simulationStopwatch.Elapsed.TotalSeconds:F2} seconds");

                return new SimulationResult(
                    GetSimulationStatus(),
                    _simulationStats,
                    _simulationStopwatch.Elapsed,
                    ExecutedMoves,
                    _costCalculator
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Simulation failed: {ex.Message}, {ex.StackTrace}");
                return new SimulationResult(
                    SimulationStatus.Failed,
                    _simulationStats,
                    _simulationStopwatch.Elapsed,
                    ExecutedMoves,
                    _costCalculator
                );
            }
        }

        private bool ShouldContinueSimulation()
        {
            return _processedEvents < _config.TotalEvents && _currentState.BlockYardManager.GetWarehouseUtilization() <= 1;
        }

        private SimulationStatus GetSimulationStatus()
        {
            if (_processedEvents >= _config.TotalEvents)
                return SimulationStatus.Completed;
            return SimulationStatus.Terminated;
        }

        private void LogEventType(Event evt)
        {
            if (evt is NewBlockEvent)
                Console.Write("New block event");
            else if (evt is MissmoveEvent)
                Console.Write("Missmove event");
            else if (evt is ExpectedExecutionEvent)
                Console.Write("Expected execution event");
            else if (evt is BlockTargetUpdateEvent)
                Console.Write("Block target update event");
            Console.WriteLine();
        }

        public void DynamicUpdate(EEventType eventtype)
        {
            Console.WriteLine("--> Dynamic Update.....");
            _currentState.Reset(); //nullify current moves

            BlockRelocationProblemState best;
            var stopwatch = Stopwatch.StartNew();

            best = _recalculationStrategy.Recalculate(_currentState, previousState, PlannedMoves, eventtype);
            stopwatch.Stop();
            previousState = null;
            if (best == null)
                throw new InvalidOperationException($"{_config.DynamicVariant} failed to find a solution in acceptable time");

            _targetSolutionState = best;
            PlannedMoves = new Queue<Move>(best.AppliedMoves.Reverse());

            //Record state after dynamic update
            _simulationStats.RecordState(
                this,
                _currentState,
                eventType: eventtype, 
                recalculationTime: stopwatch.Elapsed,
                newSolutionMoves: PlannedMoves.Count
            );
        }


        private BlockRelocationProblemState RunStandardRecalculation()
        {
            var bestState = _currentState.BeamSearch(2, 
                state =>
                {
                    var boundValue = state.Bound.Value;
                    var blockedBlocks = state.AppliedMoves.Any()
                        ? state.BlockYardManager.GetBlockedBlocks()
                        : 0;
                    return boundValue + (blockedBlocks * 10); //penalize for blocked blocks
                },
                runtime: TimeSpan.FromSeconds(100)
            );
            if (bestState is null)
                throw new InvalidOperationException("No solution found");
            if(PlannedMoves.Count <= bestState.AppliedMoves.Count - 10)
            {
                Console.WriteLine("debug");
            }
            return bestState;
        }


		public BlockRelocationProblemState RunStandardRecalculationWithBetterStartingPoint()
        {
            //retrace moves until a move is not valid
            //then reexamine the costs of the moves
            //restart where the biggest drop of exist:

            // Check if we have a previous state to compare with
            if (previousState is null)
                throw new InvalidOperationException("No previous state to compare with");

            var originalMoves = PlannedMoves.ToList();

            // Get the optimal starting point - this tells us which moves are still valid
            var restartingPoint = restartPointFinder.FindOptimizedSolution(previousState, _currentState, PlannedMoves);

            // Start from current state
            var stateForRecalc = (BlockRelocationProblemState)_currentState.Clone();
            stateForRecalc.Reset(); // Clear existing moves

            // Get valid moves (up to restart point) with current arrival stack heights
            var validMovesCount = restartingPoint.AppliedMoves.Count;
            var validMoves = originalMoves.Take(validMovesCount).ToList();

            // Apply valid moves to our recalc state
            foreach (var move in validMoves)
            {
                var updatedMove = new Move(
                    move.CraneId,
                    move.BlockId,
                    move.CraneSourcePosition,
                    move.CraneTargetPosition,
                    move.BlockSourcePosition,
                    move.TargetPosition,
                    stateForRecalc.BlockYardManager.GetArrivalStackCount()
                );

                try
                {
                    stateForRecalc.Apply(updatedMove);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to apply move: {updatedMove}, Error: {ex.Message}");
                    // We might want to break here or handle this case
                    break;
                }
            }

            // Now run beam search from this point
            var newSolution = stateForRecalc.BeamSearch(2,
                state =>
                {
                    var boundValue = state.Bound.Value;
                    var blockedBlocks = state.AppliedMoves.Any()
                        ? state.BlockYardManager.GetBlockedBlocks()
                        : 0;
                    return boundValue + (blockedBlocks * 10);
                },
                runtime: TimeSpan.FromSeconds(1000)
            );

            if (newSolution is null)
                throw new InvalidOperationException("No solution found from restart point");

            return newSolution;

        }


        #region Methods provided for Events to use
        public void ApplyNextExpectedMove()
        {
            if (PlannedMoves.Count == 0)
            {
                throw new InvalidOperationException("No more planned moves available");
            }
            var nextMove = PlannedMoves.Dequeue();
            _currentState.Apply(nextMove);
            ExecutedMoves.Add(nextMove);
        }
        public BlockRelocationProblemState GetCurrentState() => _currentState;

        public BlockRelocationProblemState GetPlannedNextState()
        {
            if (PlannedMoves.Count == 0) return _currentState;

            var nextState = (BlockRelocationProblemState)_currentState.Clone();
            nextState.Apply(PlannedMoves.Peek());
            return nextState;
        }

        public void PlaceNewBlock(Block newBlock)
        {
            previousState = (BlockRelocationProblemState)_currentState.Clone();
            _currentState.BlockYardManager.AddToArrivalStack(newBlock);

            //triggers a recalculation/insertion heuristic
            DynamicUpdate(EEventType.NewBlockEvent);
        }

        public void HandleMissmove(Move missMove)
        {
            if (PlannedMoves.Count == 0)
                throw new Exception("No more moves to handle missmove");

            previousState = (BlockRelocationProblemState)_currentState.Clone();
            Move originalMove = PlannedMoves.Dequeue(); //discarding original move
            previousState.Apply(originalMove);

            var nextState = (BlockRelocationProblemState)_currentState.Clone();
            nextState.Apply(missMove);
            ExecutedMoves.Add(missMove);
            _currentState = nextState;

            //triggers a recalculation/insertion heuristic
            //careful moves are planned invalided
            DynamicUpdate(EEventType.MissmoveEvent);
        }

        public void UpdateBlockTarget(Block blockToUpdate)
        {
            if (blockToUpdate == null) //no change if no block is provided
            {
                ApplyNextExpectedMove();
                return;
            }
            previousState = (BlockRelocationProblemState)_currentState.Clone();

            blockToUpdate.AddConstraint(new BlockTargetPositionConstraint(BlockArea.VOID_POSITION));
            DynamicUpdate(EEventType.BlockTargetUpdateEvent);
        }

        #endregion
        #region Helper methods for Stats and Simulation
        private void HandleEvent(Event evt)
        {
            var eventType = GetEventType(evt);

            // Skip invalid events
            if ((_currentState.IsTerminal && evt is ExpectedExecutionEvent) 
                || (_currentState.IsTerminal || PlannedMoves.Count == 0) && !(evt is NewBlockEvent || evt is BlockTargetUpdateEvent))
			{
				_simulationStats.RecordState(
				    this,
				    _currentState,
				    eventType,
				    recalculationTime: null,
				    newSolutionMoves: PlannedMoves?.Count
			    );
                return;

			}

            if ((_currentState.IsTerminal || PlannedMoves.Count == 0) &&
                !(evt is NewBlockEvent || evt is BlockTargetUpdateEvent))
                return;

            // Execute event
            evt.Execute(this);

            // Record state after event and any dynamic updates
            if(eventType == EEventType.ExpectedExecutionEvent)
            {
				_simulationStats.RecordState(
	                this,
	                _currentState,
	                eventType,
	                recalculationTime: null,
	                newSolutionMoves: PlannedMoves?.Count
                );
			}
            
        }

        private EEventType GetEventType(Event evt) => evt switch
        {
            NewBlockEvent => EEventType.NewBlockEvent,
            MissmoveEvent => EEventType.MissmoveEvent,
            ExpectedExecutionEvent => EEventType.ExpectedExecutionEvent,
            BlockTargetUpdateEvent => EEventType.BlockTargetUpdateEvent,
            _ => throw new ArgumentException($"Unknown event type: {evt.GetType()}")
        };

        #endregion
    }
}
