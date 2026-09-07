# Copyright (C) 2026 Power! contributors
# Licensed under GPL-3.0-or-later with the Unity Linking Exception.
# See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

"""Exercise the JSON authoring boundary against the real shared-library ABI."""

import copy
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
LIBRARY = Path(sys.argv.pop(1)).resolve()
spec = importlib.util.spec_from_file_location("model_lab", ROOT / "tools/model_lab.py")
lab = importlib.util.module_from_spec(spec)
spec.loader.exec_module(lab)


class ModelLabTests(unittest.TestCase):
    def setUp(self):
        self.document = lab.read_document(ROOT / "assets/labs/electrothermal.power.json")

    def test_real_model_replay_and_regeneration(self):
        report = lab.evaluate(self.document, LIBRARY)
        self.assertTrue(report["passed"])
        self.assertTrue(report["replay"]["sample_hashes_match"])
        self.assertEqual(report["model"]["calibration"], "unverified")
        self.assertEqual(report["model"]["state_count"], 7)
        source_work = str(2**63 | lab.FIELDS["source_work"])
        current = str(2**63 | 10 << 8 | lab.FIELDS["current"])
        by_time = {sample["time_ns"]: sample["values"] for sample in report["samples"]}
        self.assertLess(by_time[6_000_000_000][source_work], by_time[5_000_000_000][source_work])
        self.assertLess(by_time[6_000_000_000][current], 0)
        self.assertEqual(report["samples"][-1]["time_ns"], 10_000_000_000)

    def test_canonical_input_order(self):
        first = lab.evaluate(self.document, LIBRARY)
        self.document["nodes"].reverse()
        self.document["components"].reverse()
        second = lab.evaluate(self.document, LIBRARY)
        self.assertEqual(first["model"], second["model"])
        self.assertEqual(first["samples"], second["samples"])

    def test_native_compile_error_and_cleanup(self):
        self.document["nodes"][0]["storage"]["unit"] = "nm"
        for _ in range(70):  # Failed compilation must not consume model/context slots.
            with self.assertRaisesRegex(ValueError, "object 1, field storage"):
                lab.evaluate(self.document, LIBRARY)
        self.document["nodes"][0]["storage"]["unit"] = "kg_m2"
        self.assertTrue(lab.evaluate(self.document, LIBRARY)["passed"])

    def test_authoring_errors(self):
        mutations = [
            lambda d: d["nodes"][0].update(id=2**32),
            lambda d: d["nodes"][0].update(units="kg_m2"),
            lambda d: d["nodes"][0].pop("position"),
            lambda d: d["nodes"][0]["storage"].update(value=float("nan")),
            lambda d: d["nodes"][0]["storage"].update(value=True),
            lambda d: d.update(step_ns=True),
            lambda d: d["components"][0].update(input_channel=2**64 + 100),
            lambda d: d["components"][0]["parameters"].update(unknown=1),
            lambda d: d["experiment"].update(sample_every_ns=1),
            lambda d: d["experiment"]["events"].reverse(),
            lambda d: d["experiment"]["events"][0].update(time_ns=1),
            lambda d: d["experiment"]["events"][0]["values"][0].update(channel=999),
            lambda d: d["experiment"]["checks"][0].update(object_id=999),
            lambda d: d["experiment"]["checks"][0].update(min=100, max=1),
            lambda d: d["experiment"]["checks"][0].update(abs_max=-1),
        ]
        for mutate in mutations:
            with self.subTest(mutation=mutate):
                candidate = copy.deepcopy(self.document)
                mutate(candidate)
                with self.assertRaises(ValueError):
                    lab.evaluate(candidate, LIBRARY)

    def test_failed_kpi_is_a_report_and_nonzero_exit(self):
        self.document["experiment"]["checks"][0]["min"] = 30
        with tempfile.TemporaryDirectory() as tmp:
            model = Path(tmp) / "candidate.power.json"
            output = Path(tmp) / "report.json"
            model.write_text(json.dumps(self.document))
            result = subprocess.run([sys.executable, str(ROOT / "tools/model_lab.py"), str(model),
                                     "--library", str(LIBRARY), "--output", str(output)],
                                    capture_output=True, text=True, check=False)
            self.assertEqual(result.returncode, 2, result.stderr)
            report = json.loads(output.read_text())
            self.assertFalse(report["passed"])
            self.assertFalse(report["checks"][0]["passed"])
            self.assertTrue(report["replay"]["sample_hashes_match"])

    def test_json_limits(self):
        with tempfile.TemporaryDirectory() as tmp:
            model = Path(tmp) / "invalid.json"
            for contents in ('{"schema": 1, "schema": 2}', '{"value": NaN}', ' ' * 1_048_577):
                model.write_text(contents)
                with self.assertRaises(ValueError):
                    lab.read_document(model)


if __name__ == "__main__":
    unittest.main()
