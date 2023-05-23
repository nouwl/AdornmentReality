using System.Collections;
using UnityEngine;

/// <summary>
/// Example-quick bot script by Matej Vanco 2020
/// </summary>
public class ARExample_BotController : MonoBehaviour
{
    public Transform myVirtualAxisTrans;
    public Animation myAnimation;
    [Space]
    public float movementSpeed = 4f;
    public float rotationSpeed = 2f;
    private CharacterController charact;

    private bool waving;

    private void Start()
    {
        charact = GetComponent<CharacterController>();
        myVirtualAxisTrans.position = Vector3.zero;
    }

    private void Update()
    {
        Vector2 virtualAxisDimens = myVirtualAxisTrans.transform.localPosition;
        float pos = (Mathf.Round(virtualAxisDimens.y/3)*3) * movementSpeed * Time.deltaTime;
        float rot = (Mathf.Round(virtualAxisDimens.x / 3)*3) * rotationSpeed * Time.deltaTime;
        Vector3 dir = new Vector3(0, 0, -pos);
        if (!waving)
        {
            if (Mathf.Abs(pos) > 0.5f)
                myAnimation.CrossFade("BotRiding");
            else
                myAnimation.CrossFade("BotNeutral");
        }
        dir = transform.TransformDirection(dir);
        if (!charact.isGrounded)
            dir.y -= 6f;
        charact.Move(dir * Time.deltaTime);
        transform.Rotate(0, rot, 0);
    }

    public void ResetScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    public void Wave()
    {
        if (waving)
            return;
        waving = true;
        myAnimation.CrossFade("BotWaving");
        StartCoroutine(stopWaving());
    }

    private IEnumerator stopWaving()
    {
        yield return new WaitForSeconds(3.5f);
        waving = false;
    }
}
