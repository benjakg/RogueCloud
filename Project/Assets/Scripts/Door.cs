using System.Collections;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public GameObject door;
  

    public GameObject LeftDoor;
    public GameObject RightDoor;

    [Header("Offsets y tiempos")]
    public float leftZOffset = 0.7f;
    public float rightZOffset = -0.664f;
    public float moveDuration = 2f;
    public float holdDuration = 3f;

    bool busy = false;

    void Start()
    {
 
    }

    void Update()
    {
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           
            if (!busy) StartCoroutine(OpenHoldCloseRoutine());
        }
    }

    IEnumerator OpenHoldCloseRoutine()
    {
        if (LeftDoor == null || RightDoor == null)
            yield break;

        busy = true;

        Vector3 leftClosed = LeftDoor.transform.localPosition;
        Vector3 rightClosed = RightDoor.transform.localPosition;

        Vector3 leftOpen = leftClosed + new Vector3(0f, 0f, leftZOffset);
        Vector3 rightOpen = rightClosed + new Vector3(0f, 0f, rightZOffset);

        // Mover ambos simultáneamente hacia abierto
        yield return StartCoroutine(LerpBothLocalZ(LeftDoor.transform, RightDoor.transform, leftClosed, leftOpen, rightClosed, rightOpen, moveDuration));

        // Mantener abiertos
        yield return new WaitForSeconds(holdDuration);

        // Mover ambos simultáneamente hacia cerrado
        yield return StartCoroutine(LerpBothLocalZ(LeftDoor.transform, RightDoor.transform, leftOpen, leftClosed, rightOpen, rightClosed, moveDuration));

      

        busy = false;
    }

    IEnumerator LerpBothLocalZ(Transform leftT, Transform rightT, Vector3 leftFrom, Vector3 leftTo, Vector3 rightFrom, Vector3 rightTo, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            // Interpolamos las posiciones completas por si hay otros ejes afectados; solo cambia Z en los targets definidos arriba
            leftT.localPosition = Vector3.Lerp(leftFrom, leftTo, s);
            rightT.localPosition = Vector3.Lerp(rightFrom, rightTo, s);
            yield return null;
        }
        leftT.localPosition = leftTo;
        rightT.localPosition = rightTo;
    }
}
