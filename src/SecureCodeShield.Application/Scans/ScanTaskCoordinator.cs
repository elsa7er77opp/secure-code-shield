using System;
using System.Collections.Generic;
using SecureCodeShield.Domain.Scans;

namespace SecureCodeShield.Application.Scans;

/// <summary>
/// Coordinates safe scan-task state for presentation layers without retaining source content, paths, or findings.
/// </summary>
public sealed class ScanTaskCoordinator
{
    private readonly object _gate = new();
    private readonly Dictionary<string, TaskRecord> _tasks = new(StringComparer.Ordinal);

    public ScanProgressSnapshot Register(string taskId, int eligibleFileCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentOutOfRangeException.ThrowIfNegative(eligibleFileCount);

        lock (_gate)
        {
            if (_tasks.ContainsKey(taskId))
            {
                throw new InvalidOperationException("A task with this identifier is already registered.");
            }

            var task = new TaskRecord(ScanTaskState.Queued, eligibleFileCount, 0);
            _tasks.Add(taskId, task);
            return ScanProgressSnapshot.Create(taskId, task.State, task.EligibleFileCount, task.ProcessedFileCount);
        }
    }

    public ScanProgressSnapshot Start(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        lock (_gate)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                throw new KeyNotFoundException("The scan task is not registered.");
            }

            if (!ScanTaskStateMachine.CanTransition(task.State, ScanTaskState.Running))
            {
                throw new InvalidOperationException("The scan task cannot be started from its current state.");
            }

            var runningTask = task with { State = ScanTaskState.Running };
            _tasks[taskId] = runningTask;
            return ScanProgressSnapshot.Create(
                taskId,
                runningTask.State,
                runningTask.EligibleFileCount,
                runningTask.ProcessedFileCount);
        }
    }

    public ScanProgressSnapshot RequestCancellation(string taskId)
    {
        return Transition(taskId, ScanTaskState.Cancelling);
    }

    public ScanProgressSnapshot ConfirmCancellation(string taskId)
    {
        return Transition(taskId, ScanTaskState.Cancelled);
    }

    private ScanProgressSnapshot Transition(string taskId, ScanTaskState nextState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        lock (_gate)
        {
            if (!_tasks.TryGetValue(taskId, out var task))
            {
                throw new KeyNotFoundException("The scan task is not registered.");
            }

            if (!ScanTaskStateMachine.CanTransition(task.State, nextState))
            {
                throw new InvalidOperationException("The scan task cannot transition to the requested state.");
            }

            var transitionedTask = task with { State = nextState };
            _tasks[taskId] = transitionedTask;
            return ScanProgressSnapshot.Create(
                taskId,
                transitionedTask.State,
                transitionedTask.EligibleFileCount,
                transitionedTask.ProcessedFileCount);
        }
    }

    private sealed record TaskRecord(ScanTaskState State, int EligibleFileCount, int ProcessedFileCount);
}
