"""Run the Docker entrypoint against a local Git repository, without Docker."""
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "scripts" / "docker-sample.sh"
GIT = shutil.which("git")
SH = shutil.which("sh")
if not SH and GIT and os.name == "nt":
    candidate = Path(GIT).resolve().parents[1] / "bin" / "sh.exe"
    if candidate.exists():
        SH = str(candidate)


@unittest.skipUnless(SH and GIT, "Git and a POSIX shell are required")
class DockerSampleTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix="fastgithub-sample-")
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.work = self.root / "work"
        self.work.mkdir()
        self.ca = self.root / "ca"
        self.ca.mkdir()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        for name in ["update-ca-certificates", "sleep"]:
            mock = self.bin / name
            mock.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8", newline="\n")
            mock.chmod(0o755)
        self.repo = self.root / "source"
        self.git("init", str(self.repo))
        (self.repo / "README").write_text("local fixture\n", encoding="utf-8")
        self.git("-C", str(self.repo), "add", "README")
        self.git("-C", str(self.repo), "-c", "user.name=Fixture", "-c",
                 "user.email=fixture@example.invalid", "commit", "-m", "fixture")
        self.destination = self.root / "trusted.crt"
        self.env = {
            **os.environ,
            "PATH": str(self.bin) + os.pathsep + os.environ["PATH"],
            "FASTGITHUB_CA_DIR": str(self.ca),
            "FASTGITHUB_CA_DESTINATION": str(self.destination),
            "GIT_CONFIG_COUNT": "1",
            "GIT_CONFIG_KEY_0": "url." + self.repo.as_uri() + ".insteadOf",
            "GIT_CONFIG_VALUE_0": "https://github.com/kubernetes/kubernetes.git",
            "GIT_TERMINAL_PROMPT": "0",
        }

    def git(self, *args):
        return subprocess.run([GIT, *args], check=True, capture_output=True, text=True)

    def run_sample(self):
        return subprocess.run([SH, "-c", 'PATH="$PWD/../bin:$PATH"; export PATH; exec sh "$1"',
                               "entrypoint-test", str(SCRIPT)], cwd=self.work, env=self.env,
                              capture_output=True, text=True, timeout=15)

    def test_restart_reuses_checkout_and_preserves_local_files(self):
        (self.ca / "fastgithub.crt").write_text("CA fixture", encoding="utf-8")
        first = self.run_sample()
        self.assertEqual(first.returncode, 0, first.stderr)
        checkout = self.work / "kubernetes"
        marker = checkout / "local-file"
        marker.write_text("preserve me", encoding="utf-8")
        second = self.run_sample()
        self.assertEqual(second.returncode, 0, second.stderr)
        self.assertIn("Reusing", second.stdout)
        self.assertEqual(marker.read_text(), "preserve me")
        self.assertEqual(self.destination.read_text(), "CA fixture")
        self.assertFalse(list(self.work.glob(".fastgithub-clone.*")))

    def test_legacy_cer_certificate_is_accepted(self):
        (self.ca / "fastgithub.cer").write_text("legacy CA", encoding="utf-8")
        result = self.run_sample()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.destination.read_text(), "legacy CA")

    def test_incomplete_user_directory_is_preserved(self):
        (self.ca / "fastgithub.crt").write_text("CA", encoding="utf-8")
        checkout = self.work / "kubernetes"
        checkout.mkdir()
        marker = checkout / "local-file"
        marker.write_text("preserve me", encoding="utf-8")
        result = self.run_sample()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("not a Git checkout", result.stderr)
        self.assertEqual(marker.read_text(), "preserve me")

    def test_missing_certificate_fails_before_clone(self):
        result = self.run_sample()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("within 60 seconds", result.stderr)
        self.assertFalse((self.work / "kubernetes").exists())

    def test_failed_clone_cleans_staging_and_can_be_retried(self):
        (self.ca / "fastgithub.crt").write_text("CA", encoding="utf-8")
        self.env["GIT_CONFIG_KEY_0"] = "url.file:///nonexistent-fastgithub-fixture.insteadOf"
        result = self.run_sample()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.work / "kubernetes").exists())
        self.assertFalse(list(self.work.glob(".fastgithub-clone.*")))
        self.env["GIT_CONFIG_KEY_0"] = "url." + self.repo.as_uri() + ".insteadOf"
        retry = self.run_sample()
        self.assertEqual(retry.returncode, 0, retry.stderr)
        self.assertTrue((self.work / "kubernetes" / ".git").is_dir())


if __name__ == "__main__":
    unittest.main()
