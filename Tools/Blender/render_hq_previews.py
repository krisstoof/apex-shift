"""
Render high-quality previews for all bushcraft assets
"""
import bpy
from pathlib import Path

def setup_render():
    """Configure render settings for quality"""
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    
    # Cycles settings (Blender 5.1)
    scene.cycles.samples = 128
    scene.cycles.use_denoising = True
    scene.cycles.denoiser = 'OPENIMAGEDENOISE'
    
    # Better world lighting
    world = bpy.data.worlds["World"]
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.5

def frame_and_render(obj_name, output_path):
    """Frame a single object and render it"""
    # Deselect all
    bpy.ops.object.select_all(action='DESELECT')
    
    # Find and select object
    obj = None
    for o in bpy.data.objects:
        if obj_name in o.name and o.type == 'MESH':
            obj = o
            break
    
    if not obj:
        print(f"  ⚠ Object not found: {obj_name}")
        return
    
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    
    # Frame it
    try:
        bpy.ops.view3d.view_selected()
    except:
        pass
    
    # Set render path
    bpy.context.scene.render.filepath = str(output_path)
    
    # Render
    print(f"  Rendering: {output_path.name}...", end=" ", flush=True)
    bpy.ops.render.render(write_still=True)
    print("✓")

if __name__ == "__main__":
    print("\n📸 RENDERING HIGH-QUALITY PREVIEWS")
    print("=" * 60)
    
    # Setup
    setup_render()
    
    # Asset lists
    items = ["wood", "stone", "fiber", "grass", "meat", "hide", "bone", "berries", "torch", "spear", "bow"]
    placeables = ["campfire", "storage_box", "tent", "wall", "trap"]
    resources = ["conifer_tree", "leafy_tree", "dry_tree", "rock", "green_bush", "dry_bush", "grass_or_flower", "berry_bush"]
    
    base_path = Path(r"c:\Users\kriss\apex-shift\Assets\_Project\Art\Bushcraft")
    
    # Render items
    print("\n🎁 Items:")
    for item in items:
        output = base_path / "Items" / "Models" / f"{item}_stylized_hq_preview.png"
        frame_and_render(item, output)
    
    # Render placeables
    print("\n🏠 Placeables:")
    for placeable in placeables:
        output = base_path / "Placeables" / "Models" / f"{placeable}_stylized_hq_preview.png"
        frame_and_render(placeable, output)
    
    # Render resources
    print("\n🌲 Resources:")
    for resource in resources:
        output = base_path / "Resources" / "Models" / f"{resource}_stylized_hq_preview.png"
        frame_and_render(resource, output)
    
    print("\n" + "=" * 60)
    print("✅ PREVIEW RENDERING COMPLETE!")
