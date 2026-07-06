"""
Wrapper script to run bushcraft asset generator with proper sys.path setup
"""
import sys
from pathlib import Path

# Add Tools/Blender directory to Python path
tools_blender_dir = Path(__file__).parent.resolve()
if str(tools_blender_dir) not in sys.path:
    sys.path.insert(0, str(tools_blender_dir))

# Now import and run
from bushcraft_asset_generator import (
    generate_all_assets,
    _premium_reference_assets,
    cleanup_generated_collection,
)

if __name__ == "__main__":
    print(f"Python path: {sys.path[0]}")
    print("Starting Blender bushcraft asset generation...")
    
    cleanup_generated_collection("BushcraftGenerated")
    
    # Generate ALL 25 bushcraft assets
    print(f"Generating all 25 bushcraft assets...")
    
    records = generate_all_assets()  # Generate ALL assets (no filter)
    
    print(f"\n✓ Successfully generated {len(records)} assets!")
    for record in records:
        print(f"  - {record.asset_id}: {record.polycount} tris")
    
    print("\nAsset generation complete!")
