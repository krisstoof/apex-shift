"""
Direct refinement - no batch, just polish the scene
"""
import bpy

def refine_materials():
    """Enhance all materials"""
    print("\n🎨 Refining materials...")
    
    for mat in bpy.data.materials:
        if not mat.name.startswith("_"):  # Skip internal materials
            try:
                if not mat.use_nodes:
                    mat.use_nodes = True
            except:
                pass
            
            nodes = mat.node_tree.nodes
            links = mat.node_tree.links
            
            # Find or create BSDF
            bsdf = None
            for node in nodes:
                if node.type == 'BSDF_PRINCIPLED':
                    bsdf = node
                    break
            
            if not bsdf:
                bsdf = nodes.new(type='ShaderNodeBsdfPrincipled')
            
            # Enhance properties
            if mat.diffuse_color:
                bsdf.inputs['Base Color'].default_value = mat.diffuse_color
            
            bsdf.inputs['Roughness'].default_value = 0.80
            bsdf.inputs['Metallic'].default_value = 0.0
            
            print(f"  ✓ {mat.name}")
    
    print("✓ All materials enhanced")

def enhance_geometry():
    """Improve all mesh objects"""
    print("\n✨ Enhancing geometry...")
    
    for obj in bpy.data.objects:
        if obj.type == 'MESH' and obj.name not in ["Camera", "Light", "Cube"]:
            # Smooth shading
            for face in obj.data.polygons:
                face.use_smooth = True
            
            # Better auto-smooth (use modifier if available)
            try:
                obj.data.auto_smooth_angle = 0.785398  # 45 degrees
            except AttributeError:
                # Blender 5.1+ might use different API
                pass
            
            print(f"  ✓ {obj.name}")
    
    print("✓ Geometry enhanced")

def improve_scene_setup():
    """Better world/lighting setup"""
    print("\n💡 Improving scene setup...")
    
    # Better world
    world = bpy.data.worlds["World"]
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.3, 0.3, 0.35, 1.0)
    world.node_tree.nodes["Background"].inputs[1].default_value = 1.5
    
    print("✓ Scene optimized")

if __name__ == "__main__":
    print("\n" + "="*60)
    print("🎨 BUSHCRAFT ASSETS - FINAL POLISH")
    print("="*60)
    
    try:
        refine_materials()
        enhance_geometry()
        improve_scene_setup()
        
        bpy.ops.wm.save_mainfile()
        print("\n✅ REFINEMENT COMPLETE!")
        print("="*60)
    except Exception as e:
        print(f"\n⚠ Error: {e}")
        import traceback
        traceback.print_exc()
