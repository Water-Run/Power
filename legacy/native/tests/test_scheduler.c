#include "pwr_scheduler.h"

#include <assert.h>
#include <stddef.h>
#include <stdint.h>

typedef struct trace {
    uint32_t values[32];
    size_t count;
    bool should_fail;
    uint32_t marker;
} trace;

static bool record_task(void *user, uint64_t tick) {
    trace *task_trace = user;
    assert(task_trace != NULL);
    assert(task_trace->count < (sizeof(task_trace->values) / sizeof(task_trace->values[0])));
    task_trace->values[task_trace->count++] = task_trace->marker + (uint32_t)tick;
    return !task_trace->should_fail;
}

int main(void) {
    pwr_scheduler scheduler;
    assert(pwr_scheduler_init(&scheduler, 100000U) == PWR_SCHEDULER_OK);
    assert(pwr_scheduler_init(NULL, 100000U) == PWR_SCHEDULER_INVALID_ARGUMENT);

    trace late = {.marker = 1000U};
    trace first = {.marker = 100U};
    trace tied = {.marker = 200U};

    const pwr_task_desc late_desc = {
        .stable_id = 30U,
        .period_ticks = 2U,
        .phase_ticks = 0U,
        .priority = 20U,
        .callback = record_task,
        .user = &late,
    };
    const pwr_task_desc tied_desc = {
        .stable_id = 20U,
        .period_ticks = 1U,
        .phase_ticks = 0U,
        .priority = 10U,
        .callback = record_task,
        .user = &tied,
    };
    const pwr_task_desc first_desc = {
        .stable_id = 10U,
        .period_ticks = 1U,
        .phase_ticks = 0U,
        .priority = 10U,
        .callback = record_task,
        .user = &first,
    };

    assert(pwr_scheduler_add_task(&scheduler, &late_desc) == PWR_SCHEDULER_OK);
    assert(pwr_scheduler_add_task(&scheduler, &tied_desc) == PWR_SCHEDULER_OK);
    assert(pwr_scheduler_add_task(&scheduler, &first_desc) == PWR_SCHEDULER_OK);
    assert(pwr_scheduler_add_task(&scheduler, &first_desc) == PWR_SCHEDULER_DUPLICATE_ID);

    assert(scheduler.tasks[0].desc.stable_id == 10U);
    assert(scheduler.tasks[1].desc.stable_id == 20U);
    assert(scheduler.tasks[2].desc.stable_id == 30U);

    assert(pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK);
    assert(first.values[0] == 100U);
    assert(tied.values[0] == 200U);
    assert(late.values[0] == 1000U);

    assert(pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK);
    assert(first.count == 2U);
    assert(tied.count == 2U);
    assert(late.count == 1U);
    assert(pwr_scheduler_time_seconds(&scheduler) == 0.0002);

    tied.should_fail = true;
    assert(pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_CALLBACK_FAILED);
    assert(scheduler.failed_task_id == 20U);
    assert(pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_FAULTED);

    pwr_scheduler_reset(&scheduler);
    tied.should_fail = false;
    assert(scheduler.current_tick == 0U);
    assert(pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK);
    return 0;
}
