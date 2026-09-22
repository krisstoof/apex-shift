"""Generate only the three approval-reference bushcraft assets."""

import sys
from pathlib import Path

tools_dir = Path(__file__).parent.resolve()
if str(tools_dir) not in sys.path:
    sys.path.insert(0, str(tools_dir))

from bushcraft_asset_generator import cleanup_generated_collection, generate_all_assets


if __name__ == "__main__":
    cleanup_generated_collection("BushcraftGenerated")
    records = generate_all_assets(["wood", "spear", "campfire"])
    for record in records:
        print(f"REFERENCE_ASSET {record.asset_id} {record.polycount} tris {record.preview_path}")
