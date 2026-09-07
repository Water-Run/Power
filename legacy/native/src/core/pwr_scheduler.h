// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#ifndef PWR_CORE_SCHEDULER_H
#define PWR_CORE_SCHEDULER_H

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>

#define PWR_SCHEDULER_MAX_TASKS 64U

typedef bool (*pwr_task_callback)(void *user, uint64_t tick);

typedef enum pwr_scheduler_result {
    PWR_SCHEDULER_OK = 0,
    PWR_SCHEDULER_INVALID_ARGUMENT,
    PWR_SCHEDULER_CAPACITY_EXCEEDED,
    PWR_SCHEDULER_DUPLICATE_ID,
    PWR_SCHEDULER_CALLBACK_FAILED,
    PWR_SCHEDULER_FAULTED
} pwr_scheduler_result;

typedef struct pwr_task_desc {
    uint32_t stable_id;
    uint32_t period_ticks;
    uint32_t phase_ticks;
    uint16_t priority;
    pwr_task_callback callback;
    void *user;
} pwr_task_desc;

typedef struct pwr_scheduled_task {
    pwr_task_desc desc;
    uint64_t run_count;
    uint64_t last_run_tick;
} pwr_scheduled_task;

typedef struct pwr_scheduler {
    uint64_t base_tick_ns;
    uint64_t current_tick;
    size_t task_count;
    bool faulted;
    uint32_t failed_task_id;
    pwr_scheduled_task tasks[PWR_SCHEDULER_MAX_TASKS];
} pwr_scheduler;

pwr_scheduler_result pwr_scheduler_init(pwr_scheduler *scheduler, uint64_t base_tick_ns);
pwr_scheduler_result pwr_scheduler_add_task(pwr_scheduler *scheduler, const pwr_task_desc *desc);
pwr_scheduler_result pwr_scheduler_run_tick(pwr_scheduler *scheduler);
void pwr_scheduler_reset(pwr_scheduler *scheduler);
double pwr_scheduler_time_seconds(const pwr_scheduler *scheduler);

#endif
