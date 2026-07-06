"""
Batch refine all bushcraft assets
"""
import bpy
import subprocess
import sys
from pathlib import Path

def add_python_path():
    tools_blender_dir = Path(__file__).parent.resolve()
    if str(tools_blender_dir) not in sys.path:
        sys.path.insert(0, str(tools_blender_dir))

# Import the refine script
exec(open(Path(__file__).parent / "refine_assets.py").read())

if __name__ == "__main__":
    print("\n🎯 BATCH REFINING ALL BUSHCRAFT ASSETS")
    print("=" * 60)
    
    blend_dir = Path(r"c:\Users\kriss\apex-shift\Assets\_Project\Art\Bushcraft\Source\Blend")
    blends = sorted(blend_dir.glob("*_stylized.blend"))
    
    print(f"Found {len(blends)} asset files to refine:\n")
    for blend_file in blends:
        print(f"  • {blend_file.name}")
    
    print("\n" + "=" * 60)
    print("Refining each asset...\n")
    
    for i, blend_file in enumerate(blends, 1):
        print(f"\n[{i}/{len(blends)}] Processing: {blend_file.name}")
        
        # Clear scene
        for obj in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)
        
        # Load blend
        bpy.ops.wm.open_mainfile(filepath=str(blend_file))
        print(f"  ✓ Loaded")
        
        # Refine
        try:
            refine_all_assets()
            print(f"  ✓ Refined")
        except Exception as e:
            print(f"  ⚠ Refinement error: {e}")
        
        # Save
        bpy.ops.wm.save_mainfile()
        print(f"  ✓ Saved: {blend_file.name}")
    
    print("\n" + "=" * 60)
    print("✅ ALL ASSETS REFINED AND SAVED!")
    print("=" * 60)
