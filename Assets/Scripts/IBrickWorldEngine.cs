using UnityEngine;

namespace Assets.Scripts
{
    public interface IBrickWorldEngine
    {
        void DestroyBrick(GameObject brickGameObject);
        GameObject InsertBrick(int x, int y, int z, int type);
        void SetSideVisible(GameObject brick, int sideId, bool visible = true);
    }
}