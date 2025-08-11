using UnityEngine;

public class DeleteButtonHandler : MonoBehaviour {
    public void OnClick() {
        LevelEditorController.Instance.ToggleDeleteMode();
    }
}
