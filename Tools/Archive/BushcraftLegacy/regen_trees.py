"""
Generate improved trees only
"""
import sys
from pathlib import Path

# Add Tools/Blender directory to Python path
tools_blender_dir = Path(__file__).parent.resolve()
if str(tools_blender_dir) not in sys.path:
    sys.path.insert(0, str(tools_blender_dir))

import bpy
from bushcraft_asset_generator import (
    generate_resource_conifer_tree,
    generate_resource_leafy_tree,
    generate_resource_dry_tree,
    _polycount,
    cleanup_generated_collection,
    export_asset,
    save_blend_copy,
    render_preview,
)
from bushcraft_asset_library import create_bushcraft_materials

if __name__ == "__main__":
    print("\n🌲 REGENERATING TREES WITH IMPROVED STRUCTURE")
    print("=" * 60)
    
    # Create materials first
    print("\n🎨 Creating bushcraft materials...")
    create_bushcraft_materials()
    print("✓ Materials created")
    
    cleanup_generated_collection("BushcraftGenerated")
    
    trees = [
        ("conifer_tree", generate_resource_conifer_tree),
        ("leafy_tree", generate_resource_leafy_tree),
        ("dry_tree", generate_resource_dry_tree),
    ]
    
    for asset_id, generator in trees:
        print(f"\n📍 Generating {asset_id}...")
        
        # Clear scene
        bpy.ops.object.select_all(action='DESELECT')
        
        # Generate
        obj = generator()
        polycount = _polycount(obj)
        print(f"  ✓ Mesh: {polycount} tris")
        
        # Set as active object
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        
        # Export
        category = "resource"
        export_asset(asset_id, category)
        print(f"  ✓ Exported FBX/OBJ")
        
        # Save blend
        save_blend_copy(asset_id)
        print(f"  ✓ Saved .blend source")
        
        # Render preview
        render_preview(asset_id)
        print(f"  ✓ Rendered preview")
    
    print("\n" + "=" * 60)
    print("✅ TREES REGENERATED WITH BETTER STRUCTURE!")
    print("=" * 60)
