"""
Blender script to refine and polish all generated bushcraft assets
Improves materials, geometry, and rendering quality
"""
import bpy
import bmesh
from mathutils import Vector
import json
from pathlib import Path

def refine_materials():
    """Enhance all materials with better shaders and details"""
    print("\n🎨 Refining materials...")
    
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            mat.use_nodes = True
        
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        
        # Clear existing nodes
        nodes.clear()
        
        # Create principled shader with better settings
        bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
        output = nodes.new(type='ShaderNodeOutputMaterial')
        
        # Copy original color if available
        if mat.diffuse_color:
            bsdf.inputs['Base Color'].default_value = mat.diffuse_color
        
        # Enhanced material properties for hand-painted look
        bsdf.inputs['Roughness'].default_value = 0.85
        bsdf.inputs['Metallic'].default_value = 0.0
        
        # Add subtle subsurface scattering for organic feel
        bsdf.inputs['Subsurface Weight'].default_value = 0.05
        bsdf.inputs['Coat Weight'].default_value = 0.1
        
        links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    
    print("✓ Materials enhanced")

def enhance_geometry(obj):
    """Add subtle improvements to geometry"""
    if obj.type != 'MESH':
        return
    
    # Enable smooth shading
    for face in obj.data.polygons:
        face.use_smooth = True
    
    # Add subtle normal smoothing
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()
    
    # Auto-smooth with higher angle for better appearance
    obj.data.auto_smooth_angle = 0.8  # ~45 degrees
    
    # Improved UV if needed
    if not obj.data.uv_layers:
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.unwrap(method='ANGLE_BASED', margin_method='FRACTION', margin=0.1)
        bpy.ops.object.mode_set(mode='OBJECT')

def add_ambient_occlusion():
    """Add ambient occlusion to scene for better lighting"""
    world = bpy.data.worlds["World"]
    world.use_nodes = True
    
    bg = world.node_tree.nodes["Background"]
    ao = world.node_tree.nodes.new(type='ShaderNodeAmbientOcclusion')
    
    # Mix AO with background for subtle effect
    mix = world.node_tree.nodes.new(type='ShaderNodeMix')
    world.node_tree.links.new(ao.outputs['Color'], mix.inputs[6])
    world.node_tree.links.new(bg.outputs['Background'], mix.inputs[7])
    world.node_tree.links.new(mix.outputs['Result'], world.node_tree.nodes['World Output'].inputs['Surface'])

def improve_lighting():
    """Set up better 3-point lighting for renders"""
    # Remove old lights
    for obj in bpy.data.objects:
        if obj.type == 'LIGHT':
            bpy.data.objects.remove(obj, do_unlink=True)
    
    # Key light (main)
    key = bpy.data.lights.new(name="Key", type='SUN')
    key.energy = 2.5
    key_obj = bpy.data.objects.new("Key", key)
    bpy.context.collection.objects.link(key_obj)
    key_obj.location = (5, 5, 10)
    key_obj.rotation_euler = (-0.5, 0.5, 0)
    
    # Fill light
    fill = bpy.data.lights.new(name="Fill", type='SUN')
    fill.energy = 1.2
    fill_obj = bpy.data.objects.new("Fill", fill)
    bpy.context.collection.objects.link(fill_obj)
    fill_obj.location = (-5, -3, 5)
    
    # Back light
    back = bpy.data.lights.new(name="Back", type='SUN')
    back.energy = 1.0
    back_obj = bpy.data.objects.new("Back", back)
    bpy.context.collection.objects.link(back_obj)
    back_obj.location = (0, -8, 8)
    
    print("✓ 3-point lighting configured")

def render_hq_preview(obj, filename):
    """Render high-quality preview"""
    bpy.context.scene.render.resolution_x = 1024
    bpy.context.scene.render.resolution_y = 1024
    bpy.context.scene.render.samples = 128
    bpy.context.scene.render.use_denoising = True
    bpy.context.scene.render.filepath = str(filename)
    
    # Frame the object
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            for region in area.regions:
                if region.type == 'WINDOW':
                    with bpy.context.temp_override(area=area, region=region):
                        bpy.ops.view3d.view_all(center=True)
    
    bpy.ops.render.render(write_still=True)
    print(f"  ✓ Rendered: {filename}")

def refine_all_assets():
    """Main refinement pipeline"""
    print("\n🚀 BUSHCRAFT ASSET REFINEMENT PIPELINE")
    print("=" * 50)
    
    # Global improvements
    refine_materials()
    add_ambient_occlusion()
    improve_lighting()
    
    # Per-object improvements
    print("\n✨ Enhancing geometry...")
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and obj.name != "Cube":
            enhance_geometry(obj)
    print("✓ Geometry enhanced")
    
    print("\n📦 Refined assets ready for rendering!")
    print("=" * 50)

if __name__ == "__main__":
    refine_all_assets()
    bpy.ops.wm.save_mainfile()
    print("\n✓ Scene saved with refinements")
