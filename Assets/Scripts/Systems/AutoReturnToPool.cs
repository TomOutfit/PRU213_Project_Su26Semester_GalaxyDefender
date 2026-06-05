using UnityEngine;

public class AutoReturnToPool : MonoBehaviour
{
    public float delay = 0.25f;
    private ReturnToPoolHelper helper;

    private void Awake()
    {
        helper = GetComponent<ReturnToPoolHelper>();
    }

    private void OnEnable()
    {
        Invoke(nameof(DoReturn), delay);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void DoReturn()
    {
        if (helper != null)
        {
            helper.ReturnToPool();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
