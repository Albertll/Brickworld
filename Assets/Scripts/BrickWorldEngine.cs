using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class BrickWorldEngine : MonoBehaviour, IBrickWorldEngine
    {
        private const int PortNumber = 5000;
        private const string ServerIp = "127.0.0.1";

        public GameObject Brick1;
        public GameObject Brick2;
        public GameObject Brick3;
        public GameObject Brick4;

        private World _world;
        private GameObject[] _bricks;
        private int _insertedBrickType = 3;
        private BwTcpClient _client;
        private bool _worldInstanced;

        public void Start()
        {
            _client = new BwTcpClient(ServerIp, PortNumber);
            _client.ReadData += Client_ReadData;
            _client.SendText("get_map");

            _bricks = new [] {Brick1, Brick2, Brick3, Brick4};

            foreach (var brick in _bricks)
            {
                for (var i = 0; i < 6; i++)
                    SetSideVisible(brick, i, false);
            }

            StartCoroutine("Refresh");
            StartCoroutine("SendPosition");
        }

        private void Client_ReadData(byte[] buffer)
        {
            Debug.Log("tcp got data " + buffer.Length);
            var obj = buffer.Deserialize<object>();
            if (obj is Map)
            {
                _world = new World(this, (Map)obj, _client);

                Debug.Log("tcp got map");
                return;
            }
            if (obj is Chunk)
            {
                var chunk = (Chunk)obj;

                _world.Map.AddChunk(chunk.A, chunk.B, chunk);

                Debug.Log($"tcp got chunk { chunk.A} { chunk.B}");
                return;
            }
        }

        public GameObject InsertBrick(int x, int y, int z, int type)
        {
            return Instantiate(_bricks[type], new Vector3(x, z, y), Quaternion.identity);
        }

        public void DestroyBrick(GameObject brickGameObject)
        {
            Destroy(brickGameObject);
        }

        public void SetSideVisible(GameObject brick, int sideId, bool visible)
        {
            brick.transform.GetChild(sideId).gameObject.SetActive(visible);
        }

        private IEnumerator Refresh()
        {
            while (true)
            {
                if (_world != null)
                {
                    if (!_worldInstanced)
                    {
                        transform.position = new Vector3(_world.Map.Position.X, _world.Map.Position.Z, _world.Map.Position.Y);

                        int a, b;
                        _world.Map.GetChunkCoords(_world.Map.Position.X, _world.Map.Position.Y, out a, out b);

                        if (_world.Map.ChunkCreated(a, b) && _world.Map.GetChunk(a, b).Loaded)
                        {
                            Debug.Log($"POSITION set {_world.Map.Position.X}, {_world.Map.Position.Y}, {_world.Map.Position.Z}");
                            _worldInstanced = true;
                        }
                    }

                    if (_worldInstanced)
                        _world.Map.Position = new Point((int)transform.position.x, (int)transform.position.z, (int)transform.position.y);

                    _world.Refresh((int)transform.position.x, (int)transform.position.z, (int)transform.position.y);
                }
                yield return new WaitForSeconds(0.1F);
            }
        }

        private IEnumerator SendPosition()
        {
            while (!_worldInstanced)
            {
                yield return new WaitForSeconds(0.5F);
            }

            while (true)
            {
                _client.SendText($"position {_world.Map.Position.X} {_world.Map.Position.Y} {_world.Map.Position.Z}");
                yield return new WaitForSeconds(3F);
            }
        }
    
        public void Update()
        {
            // destroy brick
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                if (Physics.Raycast(ray, out hit, 4.0f))
                {
                    Destroy(hit.transform.parent.gameObject);
                    _world.RemoveBrick(
                        (int)hit.transform.parent.position.x,
                        (int)hit.transform.parent.position.z,
                        (int)hit.transform.parent.position.y);
                }
            }
            // create brick
            else if (Input.GetMouseButtonDown(1))
            {
                RaycastHit hit;
                var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                if (Physics.Raycast(ray, out hit, 10f))
                {
                    var newBlockPos = hit.transform.parent.position;
                    newBlockPos += hit.normal;
                    _world.AddNewBrick((int)newBlockPos.x, (int)newBlockPos.z, (int)newBlockPos.y, _insertedBrickType);
                }
            }
            // change brick type
            else if (Input.GetMouseButtonDown(2))
            {
                _insertedBrickType = (_insertedBrickType + 1) % _bricks.Length;
            }
        }
    }
}