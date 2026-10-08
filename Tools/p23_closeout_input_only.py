"""Finish only the unrecorded input branch in the already running QA scene."""
from pathlib import Path
source=Path('Tools/p23_closeout_probe.py').read_text(encoding='utf-8')
exec(source[:source.index("call('manage_editor',{'action':'stop'})")])
exec(source[source.index('# Reproduce the original input assertion'):])
