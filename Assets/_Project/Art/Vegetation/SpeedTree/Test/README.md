# ApexShift_TropicalTree_Test01

One new procedural broadleaf test asset, authored with SpeedTree MCP and regenerated/exported by installed SpeedTree Modeler 10.2.0. No world, biome, gameplay, vegetation registry or existing asset was changed. No canonical wrapper, gameplay collider or world placement was created.

## Files

- `ApexShift_TropicalTree_Test01.st9`: genuine native SpeedTree 9 export, 659,608 bytes. Verified using Unity 6000.6.2f1's native `UnityEditor.SpeedTree.Importer.SpeedTree9Importer` in an isolated project, at identity transform.
- `Bark_2_{Color,Normal,Extra}.png` and `Tree_01_{Color,Normal,Extra,Subsurface}.png`: seven exported maps, preserved under the filenames referenced inside the ST9. The MCP batch exporter uses the fixed `Tree_01` atlas basename; renaming the model does not alter these texture references.
- `Source/ApexShift_TropicalTree_Test01.spm`: editable source with five generators: Tree → Trunk → Primary → Secondary → Leaves. All 90 source dependency files are retained under `Source/Bark/` and `Source/Cluster/`, including unused seasonal alternatives stored in the inherited material library. Open the SPM directly; its relative dependencies were checked after relocation.
- `Source/ApexShift_TropicalTree_Test01_ExportOptions.ini`: actual CLI options used for the final export.
- `Source/ApexShift_TropicalTree_Test01_{InitialRecipe,RefinementRecipe}.json`: authoring operation audit, including inspected node/property IDs and seed configuration. These record the original trial paths; choose new paths when replaying them.
- `ApexShift_TropicalTree_Test01_Verification.json`: measured geometry, texture references, source/export fingerprints and warnings.

## Measurements and procedural settings

Measured Unity LOD0 bounds: **4.787 m wide × 9.486 m tall × 5.180 m deep**. Native export imports upright; minimum Y is approximately −0.002 m. Height includes the crown, rather than only the trunk.

| LOD | Vertices | Triangles | Submeshes | Transition |
| --- | ---: | ---: | ---: | ---: |
| 0 | 2,328 | 2,060 | 2 | 0.50 |
| 1 | 1,371 | 1,177 | 2 | 0.25 |
| 2 | 544 | 497 | 2 | 0.01 |

There are two generated material subassets, with the native URP SpeedTree9 shader and seven resolved texture maps. Leaf maps are 2048×2048; bark maps are 256×1024. The requested 1024 limit was not reflected in the leaf atlas output; these are measured sizes, not assumed preset results. No billboard LOD was observed.

Seed: **104201**, with recorded per-generator generation/spine seeds. The graph uses a 20-foot nominal trunk, base radius 1.1 feet with a taper profile, subtle trunk noise/curvature, nine asymmetric primary branches, five secondary branches per primary and eight broadleaf clusters per secondary. Length, angle, gravity, leaf size and leaf orientation variance are explicitly configured. Actual generated counts may differ due to inherited pruning/LOD rules; the mesh measurements above are authoritative. Existing cached thumbnail/statistics and TreeInfo describe the starter sample and are not validation evidence.

## Verification and remaining limitations

Server connection, executable detection, content/template discovery, graph editing, native export, file hashes, geometry, LODs, pivot/orientation and texture references were checked. The final MCP export exited successfully and `verify_export` returned no errors. The source template was preserved. Delivery exclusively created this previously absent Test directory; no existing vegetation assets were overwritten.

**Native shader visual QA remains open.** Isolated Unity renders with `SpeedTree9_URP` show blue/red surfaces despite valid green albedo maps. This occurred in both forward and deferred preview attempts, including synchronous shader compilation and disabled debug display modes. Do not interpret resolved texture references as an approved final material appearance.

Audit previews and logs are in ignored `Logs/SpeedTreeTest01_20261005/`: `preview-native-speedtree.png` records the issue. `preview-texture-reference.png` uses temporary URP/Lit materials on the same native geometry to inspect the actual exported textures and silhouette; it does **not** certify native SpeedTree shading or wind. Neither preview changed the exported materials or created a production prefab. Runtime wind animation, gameplay colliders, biome placement and performance at world density remain for the later Unity integration task.

The initial General-only export presets yielded a sideways model. The final export additionally uses the CLI `[Options]` schema observed in the source's saved quick-export settings; upright output was confirmed without rotating the Unity instance. The only delivered game format is ST9. ST was tested on the same tree during diagnostics but is not delivered as a second asset.

## MCP verification and content provenance

Local endpoint: `http://127.0.0.1:9877/mcp`. Adapter: `dcc-mcp-speedtree 0.1.1`; installed executable: `C:/Program Files/SpeedTree/SpeedTree Modeler v10.2.0/win64/SpeedTree_Modeler.exe`. The installed version is 10.2.0; inherited SPM metadata says 10.0.0 and is not the executable version. Actual licensed CLI export worked for this test; no wider license entitlement audit is claimed.

Discovery found 78 templates (51 generator templates and 27 project templates), 50 installed sample projects, six Games export presets and ten VFX export presets. Generator catalog inspection reported 16 stored generator types and no unreadable templates. Games project templates include Blank, Broadleaf, Cluster, Conifer, Grass, Palm, Plant, Stump and Vine. Installed sample content includes broadleaf, bush, conifer, palm and learning/VFX examples; no separately purchased tropical tree library was discovered under the selected installation root.

Implemented routes: discovery, fingerprints/preset inspection, export planning, experimental SPM/STT graph/property/curve editing (add, duplicate, remove, connect, disconnect, rename, set_hidden, set_property), official CLI export, async status polling, file verification and target handoff planning. Advertised export routes: ST9, ST, FBX, OBJ, Alembic and USD; this task actually tested ST9 and ST. Format/edition eligibility for the other routes was not tested. Full live Modeler document control, material editing, Fan/wind editing and collision authoring are unavailable; graph edits must be regenerated by Modeler. LOD handling is preset/source-driven and target import remains a separate stage. The auxiliary dynamic-tool registration route also rejected a diagnostic helper because this connector did not provide an active MCP session; normal typed authoring/export tools worked.

Geometry hierarchy was rebuilt using the installed `tree_templates/Games/Broadleaf.spm` procedural generators. Starter material library and source dependencies came from the installed `samples/Games/Bush/Bush_Desktop.spm`, using Bark_2 and the summer Leaf_Cluster_2 material. These are SpeedTree/Unity-supplied sample assets, governed by the user's installed SpeedTree license/EULA, rather than project-owned original textures. No models, textures or content were downloaded. This is a local integration test; production approval should retain this provenance record.

Native import support is documented in [Unity's SpeedTree import documentation](https://docs.unity.com/en-us/engine/6000.6/manual/creating-environments/script-terrain/terrain-trees/speed-tree/speed-tree). Synchronous shader diagnostics use the documented [ShaderUtil.CompilePass API](https://docs.unity3d.com/cn/current/ScriptReference/ShaderUtil.CompilePass.html). The verification numbers above come from actual local readback.
