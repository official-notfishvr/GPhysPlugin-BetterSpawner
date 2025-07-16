from flask import Flask, request, jsonify
from flask_cors import CORS
import json
import time
from datetime import datetime

app = Flask(__name__)
CORS(app)

objects = {}
next_object_id = 1

class GPhysObject:
    def __init__(self, object_id, object_type, position, rotation, owner_id):
        self.id = object_id
        self.object_type = object_type
        self.position = position
        self.rotation = rotation
        self.owner_id = owner_id
        self.is_detonated = False
        self.detonation_time = 0
        self.created_at = time.time()
        self.last_updated = time.time()

    def to_dict(self):
        return {
            'id': self.id,
            'object_type': self.object_type,
            'position': self.position,
            'rotation': self.rotation,
            'owner_id': self.owner_id,
            'is_detonated': self.is_detonated,
            'detonation_time': self.detonation_time,
            'created_at': self.created_at,
            'last_updated': self.last_updated
        }

@app.route('/')
def home():
    return jsonify({
        'status': 'running',
        'service': 'GPhys Networking Server',
        'version': '1.0.0',
        'objects_count': len(objects)
    })

@app.route('/about')
def about():
    return jsonify({
        'name': 'GPhys Networking Server',
        'description': 'Backend server for GPhys object synchronization',
        'version': '1.0.0',
        'author': 'GPhys Plugin Developer'
    })

@app.route('/objects', methods=['GET'])
def get_objects():
    """Get all objects"""
    object_list = [obj.to_dict() for obj in objects.values()]
    return jsonify({
        'success': True,
        'objects': object_list,
        'count': len(object_list)
    })

@app.route('/spawn_object', methods=['POST'])
def spawn_object():
    """Spawn a new GPhys object"""
    global next_object_id
    
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                'success': False,
                'message': 'No data provided'
            }), 400
        
        object_type = data.get('object_type', 'Bomb')
        position = data.get('position', {'x': 0, 'y': 0, 'z': 0})
        rotation = data.get('rotation', {'x': 0, 'y': 0, 'z': 0, 'w': 1})
        owner_id = data.get('owner_id', 'unknown')
        
        object_id = next_object_id
        next_object_id += 1
        
        obj = GPhysObject(object_id, object_type, position, rotation, owner_id)
        objects[object_id] = obj
        
        print(f"[{datetime.now()}] {object_type} {object_id} spawned by {owner_id} at {position}")
        
        return jsonify({
            'success': True,
            'message': f'{object_type} {object_id} spawned successfully',
            'object': obj.to_dict()
        })
        
    except Exception as e:
        print(f"Error spawning object: {str(e)}")
        return jsonify({
            'success': False,
            'message': f'Error spawning object: {str(e)}'
        }), 500

@app.route('/update_objects', methods=['POST'])
def update_objects():
    """Update multiple objects"""
    try:
        data = request.get_json()
        
        if not data or not isinstance(data, list):
            return jsonify({
                'success': False,
                'message': 'Invalid data format'
            }), 400
        
        updated_count = 0
        
        for object_update in data:
            object_id = object_update.get('object_id')
            if object_id in objects:
                obj = objects[object_id]
                
                if 'position' in object_update:
                    obj.position = object_update['position']
                if 'rotation' in object_update:
                    obj.rotation = object_update['rotation']
                if 'is_detonated' in object_update:
                    obj.is_detonated = object_update['is_detonated']
                    if obj.is_detonated and obj.detonation_time == 0:
                        obj.detonation_time = time.time()
                
                obj.last_updated = time.time()
                updated_count += 1
        
        return jsonify({
            'success': True,
            'message': f'Updated {updated_count} objects',
            'updated_count': updated_count
        })
        
    except Exception as e:
        print(f"Error updating objects: {str(e)}")
        return jsonify({
            'success': False,
            'message': f'Error updating objects: {str(e)}'
        }), 500

@app.route('/detonate_object', methods=['POST'])
def detonate_object():
    """Detonate a specific object"""
    try:
        data = request.get_json()
        
        if not data:
            return jsonify({
                'success': False,
                'message': 'No data provided'
            }), 400
        
        object_id = data.get('object_id')
        
        if object_id not in objects:
            return jsonify({
                'success': False,
                'message': f'Object {object_id} not found'
            }), 404
        
        obj = objects[object_id]
        obj.is_detonated = True
        obj.detonation_time = time.time()
        obj.last_updated = time.time()
        
        print(f"[{datetime.now()}] Object {object_id} detonated")
        
        return jsonify({
            'success': True,
            'message': f'Object {object_id} detonated successfully',
            'object': obj.to_dict()
        })
        
    except Exception as e:
        print(f"Error detonating object: {str(e)}")
        return jsonify({
            'success': False,
            'message': f'Error detonating object: {str(e)}'
        }), 500

@app.route('/object/<int:object_id>', methods=['GET'])
def get_object(object_id):
    """Get a specific object by ID"""
    if object_id not in objects:
        return jsonify({
            'success': False,
            'message': f'Object {object_id} not found'
        }), 404
    
    return jsonify({
        'success': True,
        'object': objects[object_id].to_dict()
    })

@app.route('/object/<int:object_id>', methods=['DELETE'])
def delete_object(object_id):
    """Delete a specific object"""
    if object_id not in objects:
        return jsonify({
            'success': False,
            'message': f'Object {object_id} not found'
        }), 404
    
    del objects[object_id]
    
    print(f"[{datetime.now()}] Object {object_id} deleted")
    
    return jsonify({
        'success': True,
        'message': f'Object {object_id} deleted successfully'
    })

@app.route('/cleanup', methods=['POST'])
def cleanup_old_objects():
    """Clean up old detonated objects (older than 30 seconds)"""
    try:
        current_time = time.time()
        objects_to_remove = []
        
        for object_id, obj in objects.items():
            if obj.is_detonated and (current_time - obj.detonation_time) > 30:
                objects_to_remove.append(object_id)
        
        for object_id in objects_to_remove:
            del objects[object_id]
        
        print(f"[{datetime.now()}] Cleaned up {len(objects_to_remove)} old objects")
        
        return jsonify({
            'success': True,
            'message': f'Cleaned up {len(objects_to_remove)} old objects',
            'cleaned_count': len(objects_to_remove)
        })
        
    except Exception as e:
        print(f"Error cleaning up objects: {str(e)}")
        return jsonify({
            'success': False,
            'message': f'Error cleaning up objects: {str(e)}'
        }), 500

@app.route('/clear_all_objects', methods=['POST'])
def clear_all_objects():
    """Clear all objects from the server"""
    try:
        global objects
        object_count = len(objects)
        objects.clear()
        
        print(f"[{datetime.now()}] Cleared all {object_count} objects from server")
        
        return jsonify({
            'success': True,
            'message': f'Cleared all {object_count} objects from server',
            'cleared_count': object_count
        })
        
    except Exception as e:
        print(f"Error clearing all objects: {str(e)}")
        return jsonify({
            'success': False,
            'message': f'Error clearing all objects: {str(e)}'
        }), 500

@app.route('/status', methods=['GET'])
def status():
    """Get server status"""
    detonated_count = sum(1 for obj in objects.values() if obj.is_detonated)
    active_count = len(objects) - detonated_count
    
    return jsonify({
        'success': True,
        'status': {
            'total_objects': len(objects),
            'active_objects': active_count,
            'detonated_objects': detonated_count,
            'server_uptime': time.time(),
            'last_cleanup': time.time()
        }
    })

if __name__ == '__main__':
    print("Starting GPhys Networking Server...")
    print("Server will be available at http://localhost:5000")
    print("Press Ctrl+C to stop the server")
    
    import threading
    def cleanup_task():
        import time
        while True:
            time.sleep(30)
            try:
                with app.test_client() as client:
                    client.post('/cleanup')
            except:
                pass
    
    cleanup_thread = threading.Thread(target=cleanup_task, daemon=True)
    cleanup_thread.start()
    
    app.run(host='0.0.0.0', port=5000, debug=False) 