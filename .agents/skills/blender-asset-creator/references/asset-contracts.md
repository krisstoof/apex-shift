# Apex Shift Blender asset contracts

## Visual target

- Stylized realistic, hand-painted bushcraft.
- Natural, rough, handmade construction.
- Strong silhouettes from an isometric gameplay camera.
- Mid-poly production geometry rather than primitive placeholders.
- Earthy materials with restrained bright accents for fire, berries and flowers.

## Geometry budgets

These are working targets, not reasons to damage silhouette quality.

| Category | Typical triangles |
| --- | ---: |
| Small pickups | 500-2,500 |
| Tools and weapons | 1,500-5,000 |
| Placeables | 3,000-12,000 |
| World resources | 2,000-12,000 |

## Category contracts

### Items and pickups

- `wood`: uneven split-log bundle with visible bark/cut contrast and fiber ties.
- `stone`: small irregular cluster, distinct from the large resource rock.
- `fiber`: readable bundle of dry fibers or primitive cord.
- `grass`: compact gatherable clump, distinct from world dressing.
- `meat`: readable raw meat without excessive gore.
- `hide`: folded or rolled skin with visible thickness and irregular edges.
- `bone`: simple light-colored bone silhouette.
- `berries`: dark-red cluster with a small leaf accent.

### Tools and weapons

- `torch`: branch shaft, fiber wrap and a restrained readable flame.
- `spear`: long uneven shaft, carved/stone/bone tip and physical binding; origin must support the held-item flow.
- `bow`: bent natural branch, visible string and wrapped grip.

### Placeables

- `campfire`: stone ring, crossed wood, ash/embers and compact flames.
- `storage_box`: rough planks, braces and handmade construction; no factory hinges.
- `tent`: branch frame with hide, grass or leaf cover and visible lashings.
- `wall`: irregular palisade with rails and rope bindings.
- `trap`: readable primitive frame, spikes and trigger mechanism.

### Resources

- `conifer_tree`: tall layered conifer silhouette with visible trunk.
- `leafy_tree`: broad crown, strong trunk and natural branching.
- `dry_tree`: dead silhouette with asymmetric branches and no healthy leaf mass.
- `rock`: large mineable formation, clearly different from pickup stone.
- `green_bush`: dense healthy foliage mass.
- `dry_bush`: sparse brittle twig silhouette.
- `grass_or_flower`: low world dressing with restrained flower accents.
- `berry_bush`: foliage with clearly visible fruit clusters.

## Technical acceptance

- Correct `{asset_id}_stylized` name.
- One or more materials assigned.
- UV layer present.
- No unapplied accidental scale that breaks export.
- Grounded base or correct grip pivot.
- Unity-compatible scale and Y-up export.
- `.blend`, `.fbx`, `.obj` and preview PNG generated.
- Manifest record generated.
- Automated validation status `pass`.
- Preview remains readable at gameplay-like isometric framing.
