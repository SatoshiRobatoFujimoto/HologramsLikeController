﻿using HoloToolkit.Unity.InputModule;
using UnityEngine;

public class RotationController : MonoBehaviour, IInputHandler, ISourceStateHandler {
    public GameObject target;

    public bool IsDraggingEnable = true;
    private bool isDragging;

    private Camera mainCamera;

    private IInputSource currentInputSource = null;
    private uint currentInputSourceId;

    public enum RotationAxis {
        x,
        y,
        z
    }

    public RotationAxis axis;

    private Vector3 startHandPosition;
    private Vector3 orthogonalRotationAxisVect;

    private void OnEnable() {
        target = transform.GetComponentInParent<TransformController>().target;
        if (target == null) {
            Debug.LogError("PositionController-OnEnable: target is not set.");
            return;
        }

        mainCamera = Camera.main;
    }

	void Update () {
        if (IsDraggingEnable && isDragging)
            UpdatedDragging();
	}

    public void StartDragging() {
        if (!IsDraggingEnable)
            return;
        if (isDragging)
            return;

        InputManager.Instance.PushModalInputHandler(gameObject);
        isDragging = true;

        currentInputSource.TryGetPosition(currentInputSourceId, out startHandPosition);

        Vector3 rotaitonAxisVect;
        switch (axis) {
            case RotationAxis.x:
                rotaitonAxisVect = target.transform.right;
                break;
            case RotationAxis.y:
                rotaitonAxisVect = target.transform.up;
                break;
            case RotationAxis.z:
                rotaitonAxisVect = target.transform.forward;
                break;
            default:
                Debug.LogError("Parameter 'axis' is not set.");
                return;
        }
        Vector3 projectionVect = Vector3.ProjectOnPlane(rotaitonAxisVect, mainCamera.transform.forward);
        projectionVect.Normalize();
        orthogonalRotationAxisVect = Vector3.Cross(mainCamera.transform.forward, projectionVect);
        orthogonalRotationAxisVect.Normalize();
    }

    public void UpdatedDragging() {
        Vector3 newHandPosition;
        currentInputSource.TryGetPosition(currentInputSourceId, out newHandPosition);
        Vector3 moveVect = newHandPosition - startHandPosition;

        Vector3 projectMoveVect = Vector3.Project(moveVect, orthogonalRotationAxisVect);

        float rotationVal = Vector3.Dot(projectMoveVect, orthogonalRotationAxisVect) * TransformControlManager.Instance.rotationSpeed;

        target.transform.Rotate(
            axis == RotationAxis.x ? rotationVal : 0,
            axis == RotationAxis.y ? rotationVal : 0,
            axis == RotationAxis.z ? rotationVal : 0
            );
    }

    public void StopDragging() {
        if (!isDragging)
            return;
        InputManager.Instance.PopModalInputHandler();
        isDragging = false;
        currentInputSource = null;
    }

    #region IInputHandler
    public void OnInputUp(InputEventData eventData) {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            StopDragging();
    }

    public void OnInputDown(InputEventData eventData) {
        if (isDragging)
            return;

        if (!eventData.InputSource.SupportsInputInfo(eventData.SourceId, SupportedInputInfo.Position))
            return;

        currentInputSource = eventData.InputSource;
        currentInputSourceId = eventData.SourceId;

        StartDragging();
    }
    #endregion

    #region ISourceStateHandler
    public void OnSourceDetected(SourceStateEventData eventData) {
        // Nothing to do.
    }

    public void OnSourceLost(SourceStateEventData eventData) {
        if (currentInputSource != null && eventData.SourceId == currentInputSourceId)
            StopDragging();
    }
    #endregion
}