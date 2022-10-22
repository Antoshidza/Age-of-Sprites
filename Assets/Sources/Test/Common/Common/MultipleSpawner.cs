using UnityEngine;

namespace NSprites
{
    public class MultipleSpawner : MonoBehaviour
    {
        [SerializeField] private int _count;
        [SerializeField] private GameObject _object;

        private void Awake()
        {
            for (int i = 0; i < _count; i++)
                GameObject.Instantiate(_object);
        }
    }
}