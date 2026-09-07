#!/usr/bin/env python3
"""Offline JSON experiment host for libpower. Uses only the public C ABI.

This tool is a development frontend, not the planned Lua asset sandbox.
No model service, API key, native extension loading from assets, or dependencies.
"""

import argparse
import ctypes as C
import hashlib
import json
import math
from pathlib import Path
import platform
import sys
import time

U32, U64, F64 = C.c_uint32, C.c_uint64, C.c_double
STATUS = C.c_int32
UNITS = {name: i for i, name in enumerate((
    "none", "kg_m2", "rad", "rad_s", "nm", "nm_rad", "nm_s_rad", "k",
    "j_k", "w_k", "ohm", "h", "nm_a", "a", "v", "j", "rpm", "deg"))}
FIELDS = {"angle": 1, "speed": 2, "temperature": 3, "current": 4, "twist": 5,
          "torque": 6, "source_work": 16, "heat_rejected": 17,
          "stored_energy_change": 18, "energy_residual": 19}
KINDS = {"shaft": 1, "dc_motor": 2, "torque_source": 3, "thermal_link": 4}


class Quantity(C.Structure):
    _fields_ = [("value", F64), ("unit", U32), ("reserved", U32)]


class Node(C.Structure):
    _fields_ = [("id", U32), ("domain", U32), ("storage", Quantity),
                ("initial", Quantity), ("position", Quantity)]


class Shaft(C.Structure):
    _fields_ = [("stiffness", Quantity), ("damping", Quantity),
                ("rest_angle", Quantity), ("ratio", F64)]


class Motor(C.Structure):
    _fields_ = [(name, Quantity) for name in
                ("resistance", "inductance", "coupling", "initial_current")]


class Thermal(C.Structure):
    _fields_ = [("conductance", Quantity), ("ambient_temperature", Quantity)]


class Parameters(C.Union):
    _fields_ = [("shaft", Shaft), ("motor", Motor), ("thermal", Thermal)]


class Component(C.Structure):
    _fields_ = [(name, U32) for name in
                ("id", "kind", "node_a", "node_b", "heat_node", "reserved")] + [
                ("input_channel", U64), ("initial_input", Quantity), ("parameters", Parameters)]


class ModelDesc(C.Structure):
    _fields_ = [("abi_version", U32), ("struct_size", U32), ("schema_version", U32),
                ("flags", U32), ("step_ns", U64), ("nodes", C.POINTER(Node)),
                ("node_count", U32), ("component_count", U32), ("components", C.POINTER(Component))]


class Diagnostic(C.Structure):
    _fields_ = [("abi_version", U32), ("struct_size", U32), ("code", U32),
                ("object_id", U32), ("field", C.c_char * 40), ("message", C.c_char * 160)]


class ModelInfo(C.Structure):
    _fields_ = [("abi_version", U32), ("struct_size", U32), ("fingerprint", U64),
                ("step_ns", U64)] + [(name, U32) for name in
                ("node_count", "component_count", "state_count", "channel_count", "fidelity", "validation")]


class Channel(C.Structure):
    _fields_ = [("channel", U64)] + [(name, U32) for name in
                ("object_id", "direction", "unit", "reserved")] + [("quantity", C.c_char * 32)]


class Scalar(C.Structure):
    _fields_ = [("channel", U64), ("value", F64), ("flags", U32), ("reserved", U32)]


class InputFrame(C.Structure):
    _fields_ = [("abi_version", U32), ("struct_size", U32), ("values", C.POINTER(Scalar)),
                ("value_count", U32), ("flags", U32)]


class Snapshot(C.Structure):
    _fields_ = [("abi_version", U32), ("struct_size", U32), ("simulation_time_ns", U64),
                ("state_hash", U64), ("diagnostic_bits", U64), ("values", C.POINTER(Scalar)),
                ("value_capacity", U32), ("value_count", U32)]


class ContextDesc(C.Structure):
    _fields_ = [(name, U32) for name in ("abi_version", "struct_size", "flags", "reserved")]


class Version(C.Structure):
    _fields_ = [(name, U32) for name in ("abi_version", "struct_size", "major", "minor", "patch", "reserved")] + [
                ("product_name", C.c_char_p), ("version_string", C.c_char_p)]


class Api(C.Structure):
    _fields_ = [
        ("abi_version", U32), ("struct_size", U32),
        ("runtime_get_version", C.CFUNCTYPE(STATUS, C.POINTER(Version))),
        ("runtime_get_capabilities", C.c_void_p),
        ("context_create", C.CFUNCTYPE(STATUS, C.POINTER(ContextDesc), C.POINTER(U64))),
        ("context_destroy", C.CFUNCTYPE(STATUS, U64)),
        ("status_string", C.CFUNCTYPE(C.c_char_p, STATUS)),
        ("instance_create", C.c_void_p),
        ("instance_destroy", C.CFUNCTYPE(STATUS, U64)),
        ("instance_submit_inputs", C.CFUNCTYPE(STATUS, U64, C.POINTER(InputFrame))),
        ("instance_step", C.CFUNCTYPE(STATUS, U64, U64)),
        ("instance_read_snapshot", C.CFUNCTYPE(STATUS, U64, C.POINTER(Snapshot))),
        ("model_compile", C.CFUNCTYPE(STATUS, U64, C.POINTER(ModelDesc), C.POINTER(U64), C.POINTER(Diagnostic))),
        ("model_destroy", C.CFUNCTYPE(STATUS, U64)),
        ("model_get_info", C.CFUNCTYPE(STATUS, U64, C.POINTER(ModelInfo))),
        ("model_get_channels", C.CFUNCTYPE(STATUS, U64, C.POINTER(Channel), U32, C.POINTER(U32))),
        ("instance_create_from_model", C.CFUNCTYPE(STATUS, U64, C.POINTER(U64))),
    ]


def initialized(cls, **fields):
    result = cls()
    result.abi_version, result.struct_size = 1, C.sizeof(cls)
    for name, value in fields.items():
        setattr(result, name, value)
    return result


def keys(obj, required, optional=(), path="document"):
    if not isinstance(obj, dict):
        raise ValueError(f"{path}: expected an object")
    missing, extra = set(required) - obj.keys(), obj.keys() - set(required) - set(optional)
    if missing or extra:
        raise ValueError(f"{path}: missing fields {sorted(missing)}, unknown fields {sorted(extra)}")


def integer(value, maximum, path, minimum=0):
    if type(value) is not int or not minimum <= value <= maximum:
        raise ValueError(f"{path}: expected an integer in [{minimum}, {maximum}]")
    return value


def number(value, path):
    if type(value) not in (int, float):
        raise ValueError(f"{path}: expected a finite number")
    try:
        result = float(value)
    except OverflowError as error:
        raise ValueError(f"{path}: number exceeds binary64 range") from error
    if not math.isfinite(result):
        raise ValueError(f"{path}: expected a finite number")
    return result


def quantity(obj, path):
    keys(obj, ("value", "unit"), path=path)
    if not isinstance(obj["unit"], str) or obj["unit"] not in UNITS:
        raise ValueError(f"{path}: unknown unit")
    return Quantity(number(obj["value"], path), UNITS[obj["unit"]], 0)


def descriptors(document):
    keys(document, ("schema", "step_ns", "nodes", "components", "experiment"), ("description",))
    if document["schema"] != "power.model.v1":
        raise ValueError("schema: expected power.model.v1")
    for name, limit, minimum in (("nodes", 32, 1), ("components", 64, 0)):
        if not isinstance(document[name], list) or not minimum <= len(document[name]) <= limit:
            raise ValueError(f"{name}: expected between {minimum} and {limit} descriptors")
    nodes = (Node * len(document["nodes"]))()
    for index, src in enumerate(document["nodes"]):
        path = f"nodes[{index}]"
        keys(src, ("id", "domain", "storage", "initial"), ("position",), path)
        n = nodes[index]
        n.id = integer(src["id"], 2**32 - 1, path + ".id", 1)
        if src["domain"] not in ("rotational", "thermal"):
            raise ValueError(f"{path}: unsupported domain")
        n.domain = 1 if src["domain"] == "rotational" else 2
        n.storage, n.initial = quantity(src["storage"], path + ".storage"), quantity(src["initial"], path + ".initial")
        if n.domain == 1 and "position" not in src:
            raise ValueError(f"{path}: rotational position must be explicit")
        if "position" in src:
            n.position = quantity(src["position"], path + ".position")
    components = (Component * len(document["components"]))()
    for index, src in enumerate(document["components"]):
        path = f"components[{index}]"
        keys(src, ("id", "kind", "node_a"),
             ("node_b", "heat_node", "input_channel", "initial_input", "parameters"), path)
        c = components[index]
        for field in ("id", "node_a", "node_b", "heat_node"):
            setattr(c, field, integer(src.get(field, 0), 2**32 - 1, path + "." + field,
                                     1 if field in ("id", "node_a") else 0))
        if not isinstance(src["kind"], str) or src["kind"] not in KINDS:
            raise ValueError(f"{path}: unsupported component kind")
        c.kind = KINDS[src["kind"]]
        c.input_channel = integer(src.get("input_channel", 0), 2**63 - 1, path + ".input_channel")
        if "initial_input" in src:
            c.initial_input = quantity(src["initial_input"], path + ".initial_input")
        parameters = src.get("parameters", {})
        target = {1: c.parameters.shaft, 2: c.parameters.motor, 4: c.parameters.thermal}.get(c.kind)
        expected = [name for name, _ in target._fields_] if target is not None else []
        if c.kind == 4 and c.node_b != 0:
            expected.remove("ambient_temperature")
        keys(parameters, expected, path=path + ".parameters")
        for field in expected:
            setattr(target, field, number(parameters[field], path + ".ratio") if field == "ratio"
                    else quantity(parameters[field], path + "." + field))
    desc = initialized(ModelDesc, schema_version=1,
                       step_ns=integer(document["step_ns"], 10**9, "step_ns", 1),
                       nodes=nodes, node_count=len(nodes), components=components, component_count=len(components))
    desc._keepalive = (nodes, components)
    return desc


def experiment(document, step_ns):
    exp = document["experiment"]
    keys(exp, ("duration_ns", "sample_every_ns"), ("events", "checks"), "experiment")
    duration = integer(exp["duration_ns"], 3_600_000_000_000, "duration_ns", 1)
    sample = integer(exp["sample_every_ns"], duration, "sample_every_ns", 1)
    if duration % step_ns or sample % step_ns or duration // step_ns > 10_000_000:
        raise ValueError("experiment: times must align with step_ns; maximum 10 million ticks")
    events = exp.get("events", [])
    if not isinstance(events, list) or len(events) > 10000 or duration // sample > 10000:
        raise ValueError("experiment: maximum 10000 events and 10000 sample intervals")
    by_time = {}
    previous = -1
    for event in events:
        keys(event, ("time_ns", "values"), path="event")
        at = integer(event["time_ns"], duration - 1, "event.time_ns")
        if at % step_ns or at <= previous:
            raise ValueError("events must be strictly ordered at distinct fixed-step boundaries before duration")
        previous = at
        values = event["values"]
        if not isinstance(values, list) or not 1 <= len(values) <= 64:
            raise ValueError("event.values: expected 1..64 updates")
        normalized = []
        for v in values:
            keys(v, ("channel", "value"), path="event value")
            normalized.append((integer(v["channel"], 2**63 - 1, "channel", 1), number(v["value"], "value")))
        if len({channel for channel, _ in normalized}) != len(normalized):
            raise ValueError("event.values: duplicate channel")
        by_time[at] = normalized
    checks = exp.get("checks", [])
    if not isinstance(checks, list) or len(checks) > 256:
        raise ValueError("checks: expected at most 256 final-state checks")
    for check in checks:
        keys(check, ("object_id", "field"), ("min", "max", "abs_max"), "check")
        integer(check["object_id"], 2**32 - 1, "check.object_id")
        if not isinstance(check["field"], str) or check["field"] not in FIELDS:
            raise ValueError("check: unsupported output field")
        if not any(key in check for key in ("min", "max", "abs_max")):
            raise ValueError("check: expected at least one bound")
        for key in ("min", "max", "abs_max"):
            if key in check:
                number(check[key], "check." + key)
        if check.get("abs_max", 0) < 0 or check.get("min", -math.inf) > check.get("max", math.inf):
            raise ValueError("check: invalid bounds")
    boundaries = sorted({0, duration, *range(sample, duration, sample), *by_time})
    return boundaries, by_time, checks


def evaluate(document, library):
    desc = descriptors(document)
    boundaries, events, checks = experiment(document, desc.step_ns)
    library = Path(library).resolve(strict=True)
    native = C.CDLL(str(library))
    native.pwr_get_api.argtypes = [U32, U32, C.POINTER(Api)]
    native.pwr_get_api.restype = STATUS
    api = Api()
    if native.pwr_get_api(1, C.sizeof(api), C.byref(api)) or api.struct_size < C.sizeof(api):
        raise ValueError("libpower does not provide the compiled-model ABI extension")

    def ok(status):
        if status:
            raise ValueError(api.status_string(status).decode())

    context, model = U64(), U64()
    ok(api.context_create(C.byref(initialized(ContextDesc)), C.byref(context)))
    started = time.perf_counter()
    try:
        diagnostic = initialized(Diagnostic)
        status = api.model_compile(context, C.byref(desc), C.byref(model), C.byref(diagnostic))
        if status:
            raise ValueError(f"compile: object {diagnostic.object_id}, field {diagnostic.field.decode()}: "
                             f"{diagnostic.message.decode()} ({api.status_string(status).decode()})")
        info, version = initialized(ModelInfo), initialized(Version)
        ok(api.model_get_info(model, C.byref(info)))
        ok(api.runtime_get_version(C.byref(version)))
        channels = (Channel * info.channel_count)()
        count = U32()
        ok(api.model_get_channels(model, channels, len(channels), C.byref(count)))
        inputs = {c.channel for c in channels if c.direction == 1}
        outputs = {c.channel for c in channels if c.direction == 2}
        if any(channel not in inputs for event in events.values() for channel, _ in event):
            raise ValueError("event references an unknown input channel")
        for check in checks:
            channel = 2**63 | (check["object_id"] << 8) | FIELDS[check["field"]]
            if channel not in outputs:
                raise ValueError("check references an unknown output channel")

        def run(chunk_ticks):
            instance = U64()
            ok(api.instance_create_from_model(model, C.byref(instance)))
            samples, now = [], 0
            try:
                values = (Scalar * len(outputs))()
                snap = initialized(Snapshot, values=values, value_capacity=len(values))
                for at in boundaries:
                    while now < at:
                        delta = min(at - now, chunk_ticks * desc.step_ns)
                        ok(api.instance_step(instance, delta))
                        now += delta
                    if at in events:
                        updates = (Scalar * len(events[at]))(*(Scalar(c, v, 0, 0) for c, v in events[at]))
                        frame = initialized(InputFrame, values=updates, value_count=len(updates))
                        ok(api.instance_submit_inputs(instance, C.byref(frame)))
                    ok(api.instance_read_snapshot(instance, C.byref(snap)))
                    samples.append({"time_ns": snap.simulation_time_ns, "state_hash": f"{snap.state_hash:016x}",
                                    "values": {str(v.channel): v.value for v in values}})
            finally:
                ok(api.instance_destroy(instance))
            return samples

        samples = run(1_000_000)
        replay = run(257)  # Deliberately different batching, same input timeline.
        matched = samples == replay
        results = []
        for check in checks:
            channel = 2**63 | (check["object_id"] << 8) | FIELDS[check["field"]]
            value = samples[-1]["values"][str(channel)]
            passed = check.get("min", -math.inf) <= value <= check.get("max", math.inf)
            passed = passed and abs(value) <= check.get("abs_max", math.inf)
            results.append({**check, "value": value, "passed": passed})
        return {
            "schema": "power.experiment_report.v1", "passed": matched and all(c["passed"] for c in results),
            "runtime": {"version": version.version_string.decode(), "abi_version": 1,
                        "library_sha256": hashlib.sha256(library.read_bytes()).hexdigest(),
                        "machine": platform.machine(), "system": platform.system()},
            "model": {"fingerprint": f"{info.fingerprint:016x}", "step_ns": info.step_ns,
                      "node_count": info.node_count, "component_count": info.component_count,
                      "state_count": info.state_count, "fidelity": "linear_lumped", "calibration": "unverified"},
            "replay": {"sample_hashes_match": matched, "boundaries_checked": len(samples),
                       "batch_ticks": [1_000_000, 257]},
            "elapsed_seconds": time.perf_counter() - started,
            "channels": [{"channel": str(c.channel), "object_id": c.object_id,
                          "direction": "input" if c.direction == 1 else "output",
                          "unit": next(name for name, unit in UNITS.items() if unit == c.unit),
                          "quantity": c.quantity.decode()} for c in channels],
            "checks": results, "samples": samples,
        }
    finally:
        if model.value:
            ok(api.model_destroy(model))
        ok(api.context_destroy(context))


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON field: {key}")
        result[key] = value
    return result


def read_document(path):
    with Path(path).open("rb") as stream:
        source = stream.read(1_048_577)
    if len(source) > 1_048_576:
        raise ValueError("model document exceeds 1 MiB")
    def invalid_constant(value):
        raise ValueError(f"nonfinite JSON constant: {value}")
    return json.loads(source, object_pairs_hook=unique_object, parse_constant=invalid_constant)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("model", type=Path)
    parser.add_argument("--library", type=Path, default=Path(__file__).resolve().parents[1] / "build/libpower.so")
    parser.add_argument("--output", type=Path, help="Write the experiment report as JSON (otherwise stdout)")
    args = parser.parse_args()
    try:
        report = evaluate(read_document(args.model), args.library)
        rendered = json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n"
        if args.output:
            args.output.write_text(rendered, encoding="utf-8")
        else:
            sys.stdout.write(rendered)
        return 0 if report["passed"] else 2
    except (ValueError, OSError, RecursionError) as error:
        print(json.dumps({"passed": False, "error": str(error)}, ensure_ascii=False), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
