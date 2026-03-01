import os
import re
import math

scene_path = r'c:\Unity games\Dragon Merge\Dragon Merge\Assets\Scenes\DemoScene.unity'
sprites_path = r'c:\Unity games\Dragon Merge\Dragon Merge\Assets\Sprites\Environment'
out_path = r'c:\Unity games\Dragon Merge\Dragon Merge\diorama_items.txt'

def parse_guids():
    guid_to_path = {}
    for f in os.listdir(sprites_path):
        if f.endswith('.meta'):
            full_path = os.path.join(sprites_path, f)
            with open(full_path, 'r', encoding='utf-8') as meta_file:
                content = meta_file.read()
            m = re.search(r'guid:\s*([a-f0-9]+)', content)
            if m:
                asset_file = f[:-5]
                guid_to_path[m.group(1)] = f"Assets/Sprites/Environment/{asset_file}"
    return guid_to_path

def parse_scene():
    guid_to_path = parse_guids()
    
    with open(scene_path, 'r', encoding='utf-8') as f:
        content = f.read()

    docs = content.split('--- !u!')
    
    game_objects = {}
    transforms = {}
    sprite_renderers = {}

    for doc in docs:
        if not doc.strip(): continue
            
        lines = doc.split('\n')
        header = lines[0]
        if '&' not in header: continue
            
        file_id = header.split('&')[1].strip()
        body = '\n'.join(lines[1:])
        
        if body.startswith('GameObject:'):
            name_m = re.search(r'm_Name:\s*([^\r\n]+)', body)
            comp_m = re.findall(r'component:\s*\{fileID:\s*(-?\d+)\}', body)
            if name_m:
                game_objects[file_id] = {
                    'name': name_m.group(1).strip(),
                    'components': comp_m
                }
        elif body.startswith('Transform:'):
            pos_m = re.search(r'm_LocalPosition:.*\{x:\s*([^,]+),\s*y:\s*([^,]+)', body)
            scale_m = re.search(r'm_LocalScale:.*\{x:\s*([^,]+),\s*y:\s*([^,]+)', body)
            rot_m = re.search(r'm_LocalRotation:.*\{x:\s*([^,]+),\s*y:\s*([^,]+),\s*z:\s*([^,]+),\s*w:\s*([^}]+)\}', body)
            try:
                pos = (float(pos_m.group(1).strip()), float(pos_m.group(2).strip())) if pos_m else (0,0)
                scale = (float(scale_m.group(1).strip()), float(scale_m.group(2).strip())) if scale_m else (1,1)
                rot = (float(rot_m.group(1).strip()), float(rot_m.group(2).strip()), float(rot_m.group(3).strip()), float(rot_m.group(4).strip())) if rot_m else (0,0,0,1)
                siny_cosp = 2 * (rot[3] * rot[2] + rot[0] * rot[1])
                cosy_cosp = 1 - 2 * (rot[1] * rot[1] + rot[2] * rot[2])
                rot_z = math.atan2(siny_cosp, cosy_cosp) * (180.0 / math.pi)
                transforms[file_id] = {'pos': pos, 'scale': scale, 'rotZ': rot_z}
            except: pass
        elif body.startswith('SpriteRenderer:'):
            order_m = re.search(r'm_SortingOrder:\s*(-?\d+)', body)
            order = int(order_m.group(1).strip()) if order_m else 0
            sprite_guid_m = re.search(r'm_Sprite:.*guid:\s*([a-f0-9]+)', body)
            sprite_guid = sprite_guid_m.group(1).strip() if sprite_guid_m else None
            sprite_renderers[file_id] = {'order': order, 'guid': sprite_guid}

    results = []
    
    for go_id, go in game_objects.items():
        name = go['name']
        if name in ['Main Camera', 'Global Light 2D'] or name.startswith('DragonMergeRuntime') or name == 'Directional Light':
            continue
            
        t = None
        sr = None
        for c_ref in go['components']:
            if c_ref in transforms: t = transforms[c_ref]
            if c_ref in sprite_renderers: sr = sprite_renderers[c_ref]
                
        if not t or not sr: continue
        if not sr['guid'] or sr['guid'] not in guid_to_path: continue
            
        results.append({
            'name': name.replace('\r', '').replace('\n', ''),
            'px': t['pos'][0], 'py': t['pos'][1],
            'sx': t['scale'][0], 'sy': t['scale'][1],
            'rz': t['rotZ'], 'order': sr['order'],
            'path': guid_to_path[sr['guid']]
        })
        
    results.sort(key=lambda x: x['order'])
    
    with open(out_path, 'w', encoding='utf-8') as f:
        for r in results:
            f.write(f'new EnvItem("{r["name"]}", "{r["path"]}", {r["px"]:.3f}f, {r["py"]:.3f}f, {r["sx"]:.3f}f, {r["sy"]:.3f}f, {r["rz"]:.3f}f, {r["order"]}),\n')

if __name__ == '__main__':
    parse_scene()
