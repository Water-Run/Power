// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_scheduler.h"

#include <string.h>

static bool pwr_task_precedes(const pwr_task_desc *lhs, const pwr_task_desc *rhs) {
    if (lhs->priority != rhs->priority) {
        return lhs->priority < rhs->priority;
    }
    return lhs->stable_id < rhs->stable_id;
}

pwr_scheduler_result pwr_scheduler_init(pwr_scheduler *scheduler, uint64_t base_tick_ns) {
    if (scheduler == NULL || base_tick_ns == 0U) {
        return PWR_SCHEDULER_INVALID_ARGUMENT;
    }

    memset(scheduler, 0, sizeof(*scheduler));
    scheduler->base_tick_ns = base_tick_ns;
    return PWR_SCHEDULER_OK;
}

pwr_scheduler_result pwr_scheduler_add_task(pwr_scheduler *scheduler, const pwr_task_desc *desc) {
    if (scheduler == NULL || desc == NULL || desc->stable_id == 0U ||
        desc->period_ticks == 0U || desc->phase_ticks >= desc->period_ticks ||
        desc->callback == NULL) {
        return PWR_SCHEDULER_INVALID_ARGUMENT;
    }
    if (scheduler->faulted) {
        return PWR_SCHEDULER_FAULTED;
    }
    if (scheduler->task_count >= PWR_SCHEDULER_MAX_TASKS) {
        return PWR_SCHEDULER_CAPACITY_EXCEEDED;
    }

    for (size_t i = 0U; i < scheduler->task_count; ++i) {
        if (scheduler->tasks[i].desc.stable_id == desc->stable_id) {
            return PWR_SCHEDULER_DUPLICATE_ID;
        }
    }

    size_t insert_at = scheduler->task_count;
    while (insert_at > 0U && pwr_task_precedes(desc, &scheduler->tasks[insert_at - 1U].desc)) {
        scheduler->tasks[insert_at] = scheduler->tasks[insert_at - 1U];
        --insert_at;
    }

    scheduler->tasks[insert_at] = (pwr_scheduled_task){
        .desc = *desc,
        .run_count = 0U,
        .last_run_tick = UINT64_MAX,
    };
    ++scheduler->task_count;
    return PWR_SCHEDULER_OK;
}

pwr_scheduler_result pwr_scheduler_run_tick(pwr_scheduler *scheduler) {
    if (scheduler == NULL) {
        return PWR_SCHEDULER_INVALID_ARGUMENT;
    }
    if (scheduler->faulted) {
        return PWR_SCHEDULER_FAULTED;
    }

    for (size_t i = 0U; i < scheduler->task_count; ++i) {
        pwr_scheduled_task *task = &scheduler->tasks[i];
        const uint64_t tick = scheduler->current_tick;
        const uint64_t phase = task->desc.phase_ticks;
        const uint64_t period = task->desc.period_ticks;
        const bool due = tick >= phase && ((tick - phase) % period) == 0U;
        if (!due) {
            continue;
        }

        if (!task->desc.callback(task->desc.user, tick)) {
            scheduler->faulted = true;
            scheduler->failed_task_id = task->desc.stable_id;
            return PWR_SCHEDULER_CALLBACK_FAILED;
        }
        ++task->run_count;
        task->last_run_tick = tick;
    }

    ++scheduler->current_tick;
    return PWR_SCHEDULER_OK;
}

void pwr_scheduler_reset(pwr_scheduler *scheduler) {
    if (scheduler == NULL) {
        return;
    }

    scheduler->current_tick = 0U;
    scheduler->faulted = false;
    scheduler->failed_task_id = 0U;
    for (size_t i = 0U; i < scheduler->task_count; ++i) {
        scheduler->tasks[i].run_count = 0U;
        scheduler->tasks[i].last_run_tick = UINT64_MAX;
    }
}

double pwr_scheduler_time_seconds(const pwr_scheduler *scheduler) {
    if (scheduler == NULL) {
        return 0.0;
    }
    return (double)scheduler->current_tick * (double)scheduler->base_tick_ns * 1.0e-9;
}
