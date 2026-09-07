#ifndef PWR_CORE_GRAPH_H
#define PWR_CORE_GRAPH_H

#include <power/power.h>

#include <stdbool.h>

#define PWR_GRAPH_MAX_CHANNELS (2U * PWR_MODEL_MAX_NODES + 3U * PWR_MODEL_MAX_COMPONENTS + 4U)
#define PWR_GRAPH_NO_INDEX UINT32_MAX

typedef struct pwr_graph_node {
    uint32_t id;
    uint32_t domain;
    uint32_t state_index;
    double storage;
    double initial;
    double position;
} pwr_graph_node;

typedef struct pwr_graph_component {
    uint32_t id;
    uint32_t kind;
    uint32_t a;
    uint32_t b;
    uint32_t heat;
    uint32_t state_index;
    uint64_t input_channel;
    double initial_input;
    /* shaft: k, d, rest, ratio; motor: R, L, k, i0; thermal: G, Tamb */
    double p[4];
} pwr_graph_component;

typedef struct pwr_graph_matrix {
    uint32_t size;
    uint32_t pivots[PWR_MODEL_MAX_STATES];
    double lu[PWR_MODEL_MAX_STATES][PWR_MODEL_MAX_STATES];
} pwr_graph_matrix;

typedef struct pwr_graph {
    uint64_t step_ns;
    uint64_t fingerprint;
    double dt;
    uint32_t node_count;
    uint32_t component_count;
    uint32_t dynamic_count;
    uint32_t thermal_count;
    uint32_t channel_count;
    uint32_t output_count;
    pwr_graph_node nodes[PWR_MODEL_MAX_NODES];
    pwr_graph_component components[PWR_MODEL_MAX_COMPONENTS];
    pwr_graph_matrix dynamics;
    pwr_graph_matrix thermal;
    double constant_force[PWR_MODEL_MAX_STATES];
    double ambient_force[PWR_MODEL_MAX_NODES];
    pwr_channel_info channels[PWR_GRAPH_MAX_CHANNELS];
} pwr_graph;

typedef struct pwr_graph_state {
    uint64_t time_ns;
    double x[PWR_MODEL_MAX_STATES];
    double temperature[PWR_MODEL_MAX_NODES];
    double inputs[PWR_MODEL_MAX_COMPONENTS];
    double initial_energy;
    double source_work;
    double source_work_compensation;
    double heat_rejected;
    double heat_compensation;
} pwr_graph_state;

/* Compile writes only caller-owned scratch. Publish it only on success. */
pwr_status pwr_graph_compile(const pwr_model_desc *desc, pwr_graph *graph,
                             pwr_model_diagnostic *diagnostic);
pwr_status pwr_graph_init(const pwr_graph *graph, pwr_graph_state *state);
/* Submission and a whole multi-tick step call are transactional. */
pwr_status pwr_graph_submit(const pwr_graph *graph, pwr_graph_state *state,
                            const pwr_input_frame *frame);
pwr_status pwr_graph_step(const pwr_graph *graph, pwr_graph_state *state,
                          uint64_t delta_ns);
pwr_status pwr_graph_snapshot(const pwr_graph *graph,
                              const pwr_graph_state *state,
                              pwr_snapshot *snapshot);
uint64_t pwr_graph_state_hash(const pwr_graph *graph, const pwr_graph_state *state);

#endif
