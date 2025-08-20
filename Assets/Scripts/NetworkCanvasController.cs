using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using UnityEngine.SceneManagement;

public class NetworkCanvasController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private GameObject networkCanvas;
	[SerializeField] private PlayerInput playerInput;
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerThrowing playerThrowing;

	[Header("Settings")]
	[SerializeField] private string playerActionMapName = "Player";
	[SerializeField] private string uiActionMapName = "UI";

	[Header("Attach To Camera")]
	[SerializeField] private bool attachToPlayerCamera = true;
	[SerializeField] private AttachMode attachMode = AttachMode.ScreenSpaceCamera;
	[SerializeField] private float worldSpaceDistance = 2f;
	[SerializeField] private Vector2 worldSpaceSize = new Vector2(800f, 450f);
	[SerializeField] private float worldSpaceScale = 0.0025f;
	[SerializeField] private int canvasSortingOrder = 100;

	[Header("Visibility")]
	[SerializeField] private bool alwaysOnTop = true;

	private enum AttachMode { ScreenSpaceCamera, WorldSpace }

	private bool isOpen;
	private bool referencesResolved;

	[SerializeField] private string[] fallbackUINames = new string[] { "NetworkCanvas", "NetworkManagerUI" };

	private GameObject localPlayerGameObject;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	static void EnsureControllerExists()
	{
		if (FindObjectOfType<NetworkCanvasController>() == null)
		{
			var go = new GameObject("NetworkCanvasController");
			go.AddComponent<NetworkCanvasController>();
		}
	}

	void Awake()
	{

		ResolveNetworkCanvas();

		TryResolveLocalPlayerReferences();
		if (!referencesResolved)
		{
			StartCoroutine(ResolveLocalPlayerWhenReady());
		}

		EnsureEventSystemConfigured();

		EnsureCanvasRaycaster();
		SetOpen(false, true);
	}

	void Update()
	{
		if (networkCanvas == null)
		{
			ResolveNetworkCanvas();
		}
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
		{
			SetOpen(!isOpen, false);
		}
	}

	bool TryResolveLocalPlayerReferences()
	{
		if (playerInput != null && playerMovement != null && playerThrowing != null)
		{
			referencesResolved = true;
			return true;
		}

		GameObject localPlayerGo = null;
		var spawnManager = NetworkManager.Singleton != null ? NetworkManager.Singleton.SpawnManager : null;
		if (spawnManager != null)
		{
			var localPlayerObj = spawnManager.GetLocalPlayerObject();
			if (localPlayerObj != null)
			{
				localPlayerGo = localPlayerObj.gameObject;
				localPlayerGameObject = localPlayerGo;
			}
		}

		if (localPlayerGo == null)
		{
			var allNOs = FindObjectsOfType<NetworkObject>(true);
			for (int i = 0; i < allNOs.Length; i++)
			{
				if (allNOs[i].IsLocalPlayer || allNOs[i].IsOwner)
				{
					localPlayerGo = allNOs[i].gameObject;
					localPlayerGameObject = localPlayerGo;
					break;
				}
			}
		}

		PlayerInput candidateInput = null;
		PlayerMovement candidateMove = null;
		PlayerThrowing candidateThrow = null;

		if (localPlayerGo != null)
		{
			candidateInput = localPlayerGo.GetComponent<PlayerInput>();
			candidateMove = localPlayerGo.GetComponent<PlayerMovement>();
			candidateThrow = localPlayerGo.GetComponent<PlayerThrowing>();
			if (candidateInput == null)
			{
				candidateInput = localPlayerGo.GetComponentInChildren<PlayerInput>(true);
			}
			if (candidateMove == null)
			{
				candidateMove = localPlayerGo.GetComponentInChildren<PlayerMovement>(true);
			}
			if (candidateThrow == null)
			{
				candidateThrow = localPlayerGo.GetComponentInChildren<PlayerThrowing>(true);
			}
		}

		if (candidateInput == null)
		{
			var allInputs = FindObjectsOfType<PlayerInput>(true);
			for (int i = 0; i < allInputs.Length; i++)
			{
				var no = allInputs[i].GetComponentInParent<NetworkObject>();
				if (no != null && (no.IsLocalPlayer || no.IsOwner))
				{
					candidateInput = allInputs[i];
					break;
				}
			}
			if (candidateInput == null && allInputs.Length == 1)
			{
				candidateInput = allInputs[0];
			}
		}

		if (candidateMove == null && candidateInput != null)
		{
			candidateMove = candidateInput.GetComponentInParent<PlayerMovement>(true);
		}
		if (candidateThrow == null && candidateInput != null)
		{
			candidateThrow = candidateInput.GetComponentInParent<PlayerThrowing>(true);
		}

		if (candidateInput != null) playerInput = candidateInput;
		if (candidateMove != null) playerMovement = candidateMove;
		if (candidateThrow != null) playerThrowing = candidateThrow;

		referencesResolved = playerInput != null && playerMovement != null && playerThrowing != null;
		if (referencesResolved)
		{
			SetOpen(isOpen, true);
		}
		return referencesResolved;
	}

	void ResolveNetworkCanvas()
	{
		if (networkCanvas != null) return;
		GameObject go = null;
		for (int i = 0; i < fallbackUINames.Length && go == null; i++)
		{
			go = GameObject.Find(fallbackUINames[i]);
			if (go == null)
			{
				go = FindInActiveSceneByName(fallbackUINames[i]);
			}
		}
		if (go == null)
		{
			var uiComp = FindObjectOfType<NetworkManagerUI>(true);
			if (uiComp != null)
			{
				var canvas = uiComp.GetComponentInParent<Canvas>(true);
				go = canvas != null ? canvas.gameObject : uiComp.gameObject;
			}
		}
		if (go != null)
		{
			networkCanvas = GetCanvasRoot(go);
			EnsureCanvasRaycaster();
		}
	}

	GameObject GetCanvasRoot(GameObject candidate)
	{
		if (candidate == null) return null;
		var canvas = candidate.GetComponentInParent<Canvas>(true);
		if (canvas != null) return canvas.gameObject;
		return candidate;
	}

	GameObject FindInActiveSceneByName(string targetName)
	{
		var scene = SceneManager.GetActiveScene();
		if (!scene.IsValid()) return null;
		var roots = scene.GetRootGameObjects();
		for (int i = 0; i < roots.Length; i++)
		{
			var t = FindInChildrenByName(roots[i].transform, targetName);
			if (t != null) return t.gameObject;
		}
		return null;
	}

	Transform FindInChildrenByName(Transform parent, string targetName)
	{
		if (parent == null) return null;
		if (parent.name == targetName) return parent;
		for (int i = 0; i < parent.childCount; i++)
		{
			var r = FindInChildrenByName(parent.GetChild(i), targetName);
			if (r != null) return r;
		}
		return null;
	}

	IEnumerator ResolveLocalPlayerWhenReady()
	{
		while (!TryResolveLocalPlayerReferences())
		{
			yield return null;
		}
	}

	void SetOpen(bool open, bool initialize)
	{
		isOpen = open;

		if (networkCanvas != null)
		{
			networkCanvas.SetActive(isOpen);
			if (isOpen)
			{
				EnsureCanvasLayoutAndOrder();
			}
		}

		Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
		Cursor.visible = isOpen;

		if (playerInput != null)
		{
			var targetMap = isOpen ? uiActionMapName : playerActionMapName;
			if (!string.IsNullOrEmpty(targetMap))
			{
				try
				{
					playerInput.SwitchCurrentActionMap(targetMap);
				}
				catch { }
			}
		}

		if (playerMovement != null)
		{
			playerMovement.enabled = !isOpen;
		}
		if (playerThrowing != null)
		{
			playerThrowing.enabled = !isOpen;
		}

		if (!initialize)
		{
			EnsureEventSystemConfigured();
		}
	}

	void EnsureEventSystemConfigured()
	{
		var systems = FindObjectsOfType<EventSystem>(true);
		EventSystem activeSystem = null;
		for (int i = 0; i < systems.Length; i++)
		{
			if (activeSystem == null)
			{
				activeSystem = systems[i];
			}
			else
			{
				systems[i].gameObject.SetActive(false);
			}
		}

		if (activeSystem == null)
		{
			var go = new GameObject("EventSystem", typeof(EventSystem));
			activeSystem = go.GetComponent<EventSystem>();
		}

		var legacy = activeSystem.GetComponent<StandaloneInputModule>();
		if (legacy != null) legacy.enabled = false;

		var uiModule = activeSystem.GetComponent<InputSystemUIInputModule>();
		if (uiModule == null)
		{
			uiModule = activeSystem.gameObject.AddComponent<InputSystemUIInputModule>();
		}
		if (playerInput != null)
		{
			uiModule.actionsAsset = playerInput.actions;
		}
	}

	void EnsureCanvasRaycaster()
	{
		if (networkCanvas == null) return;
		var canvas = networkCanvas.GetComponent<Canvas>();
		if (canvas == null)
		{
			networkCanvas.AddComponent<Canvas>();
		}
		if (networkCanvas.GetComponent<GraphicRaycaster>() == null)
		{
			networkCanvas.AddComponent<GraphicRaycaster>();
		}
	}

	void EnsureCanvasLayoutAndOrder()
	{
		if (networkCanvas == null) return;
		var canvas = networkCanvas.GetComponent<Canvas>();
		if (canvas != null)
		{
			if (alwaysOnTop)
			{
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
				canvas.worldCamera = null;
				canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, canvasSortingOrder);
			}
			else
			{
				var targetCamera = attachToPlayerCamera ? AcquirePlayerCamera() : null;
				if (attachToPlayerCamera && targetCamera != null)
				{
					if (attachMode == AttachMode.ScreenSpaceCamera)
					{
						canvas.renderMode = RenderMode.ScreenSpaceCamera;
						canvas.worldCamera = targetCamera;
						canvas.sortingOrder = canvasSortingOrder;
						canvas.planeDistance = 1f;
					}
					else
					{
						canvas.renderMode = RenderMode.WorldSpace;
						canvas.worldCamera = targetCamera;
						var rtWS = networkCanvas.GetComponent<RectTransform>();
						if (rtWS != null)
						{
							rtWS.SetParent(targetCamera.transform, false);
							float safeZ = Mathf.Max(targetCamera.nearClipPlane + 0.05f, worldSpaceDistance);
							rtWS.localPosition = new Vector3(0f, 0f, safeZ);
							rtWS.localRotation = Quaternion.identity;
							rtWS.sizeDelta = worldSpaceSize;
							rtWS.localScale = Vector3.one * worldSpaceScale;
							networkCanvas.layer = LayerMask.NameToLayer("UI");
						}
					}
				}
				else
				{
					canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvas.worldCamera = null;
					canvas.sortingOrder = canvasSortingOrder;
				}
			}
		}
		var rt = networkCanvas.GetComponent<RectTransform>();
		if (rt != null)
		{
			if (rt.localScale == Vector3.zero || Mathf.Approximately(rt.localScale.x, 0f) || Mathf.Approximately(rt.localScale.y, 0f))
			{
				rt.localScale = Vector3.one;
			}
			bool anchorsCollapsed = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.zero && rt.sizeDelta == Vector2.zero;
			if (anchorsCollapsed)
			{
				if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
				{
					rt.anchorMin = Vector2.zero;
					rt.anchorMax = Vector2.one;
					rt.sizeDelta = Vector2.zero;
					rt.anchoredPosition = Vector2.zero;
					if (rt.pivot == Vector2.zero)
					{
						rt.pivot = new Vector2(0.5f, 0.5f);
					}
				}
			}
		}
		var cg = networkCanvas.GetComponent<CanvasGroup>();
		if (cg != null)
		{
			cg.alpha = 1f;
			cg.interactable = true;
			cg.blocksRaycasts = true;
		}
	}

	Camera AcquirePlayerCamera()
	{
		Camera cam = null;
		if (playerThrowing != null && playerThrowing.playerCamera != null)
		{
			cam = playerThrowing.playerCamera;
		}
		if (cam == null && playerMovement != null && playerMovement.playerCamera != null)
		{
			cam = playerMovement.playerCamera.GetComponent<Camera>();
		}
		if (cam == null && playerInput != null)
		{
			cam = playerInput.GetComponentInChildren<Camera>(true);
		}
		if (cam == null && localPlayerGameObject != null)
		{
			cam = localPlayerGameObject.GetComponentInChildren<Camera>(true);
		}
		if (cam == null)
		{
			cam = Camera.main;
		}
		return cam;
	}
}


