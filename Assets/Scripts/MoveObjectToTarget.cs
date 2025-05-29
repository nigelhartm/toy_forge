
using UnityEngine;
using System.Collections;

public class MoveObjectToTarget : MonoBehaviour
{
    public Transform targetObjectOpen;
    public Transform targetObjectClose;
    private Transform targetObject;
    public float animationTime = 5.0f;

    public Transform sourceObject;
    private float timeElapsed;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private bool isMoving = false;
    private bool statusOpen = false;

    // Functionalities to cheat a bit for prototype
    private int toggleCount = 0;
    public GameObject scannerSurface;
    public ChatGPTSketch2Image chatGPTSketch2Image;
    public GameObject whiteBoard;
    public GameObject boxButton;

    void Start()
    {
    }

    void Update()
    {
        if (isMoving)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / animationTime);

            float smoothT = t * t * (3f - 2f * t);

            sourceObject.position = Vector3.Lerp(startingPosition, targetObject.position, smoothT);
            sourceObject.rotation = Quaternion.Slerp(startingRotation, targetObject.rotation, smoothT);

            if (timeElapsed >= animationTime)
            {
                isMoving = false;
                sourceObject.position = targetObject.position;
                sourceObject.rotation = targetObject.rotation;
                statusOpen = !statusOpen;

                if (toggleCount == 2) {
                    boxButton.SetActive(false);
                    whiteBoard.SetActive(false);
                    scannerSurface.SetActive(false);
                    chatGPTSketch2Image.sketch2Image();
                }
            }
        }
    }

    public void MoveToTarget(Transform tfm)
    {
        if (isMoving) return;
        targetObject = tfm;
        isMoving = true;
        timeElapsed = 0f;
        startingPosition = sourceObject.position;
        startingRotation = sourceObject.rotation;
    }

    public void toggleOpen() {
        toggleCount++;
        if (statusOpen)
        {
            MoveToTarget(targetObjectClose);
        }
        else
        {
            MoveToTarget(targetObjectOpen);
        }
    }
}
