using System.Collections.Generic;
using UnityEngine;

public delegate void KillConfirmed(Character character);
public class GameManager : MonoBehaviour 
{
    [HideInInspector] public Camera mainCamera;
    public float tabTargetRange = 20;
    public LayerMask tabTargetMask;
    public float clickInteractDistance = 1.5f;

    public event KillConfirmed killConfirmedEvent;
    private static GameManager instance;
    private Plane[] cameraPlanes;
    private GameObject parent;
    
    private List<Enemy> enemyList = new List<Enemy>();
    private List<Enemy> tabTargetEnemyList = new List<Enemy>();
    private Collider[] tabTargetableEnemies;
    private int listIndex = 0;

    public static GameManager MyInstance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update ()
    {
        ClickTarget();
        TabTarget();
	}
 
    private void ClickTarget()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = CameraManager.MyInstance.ActiveCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Enemy"))
                    {
                        DeselectTarget();
                        parent = hit.collider.transform.parent.gameObject;
                        parent.GetComponentInChildren<IInteractable>().Interact();
                    }
                    else
                    {
                        DeselectTarget();
                        enemyList.Clear();
                        listIndex = 0;
                    }
                }
            }
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = CameraManager.MyInstance.ActiveCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Interactable"))
                    {
                        if (Vector3.Distance(Player.MyInstance.gameObject.transform.position, hit.collider.gameObject.transform.position) <= clickInteractDistance)
                        {
                            hit.collider.gameObject.GetComponentInChildren<IInteractable>().Interact();
                        }
                    }
                }
            }
        }
    }

    private static void DeselectTarget()
    {
        if (Player.MyInstance.TargetSet)
        {
            Player.MyInstance.ClearTarget();
        }
    }

    public void OnKillConfirmed(Character character)
    {
        if (killConfirmedEvent != null)
        {
            killConfirmedEvent(character);
        }
    }

    private void TabTarget()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DeselectTarget();
            
            tabTargetableEnemies = Physics.OverlapSphere(Player.MyInstance.transform.position, tabTargetRange, tabTargetMask);

            foreach (Collider col in tabTargetableEnemies)
            {
                parent = col.transform.parent.gameObject;
                tabTargetEnemyList.Add(parent.GetComponentInChildren<Enemy>());
            }

            //remove any old enemies in the tab target list that were not hit
            foreach (Enemy enemy in enemyList)
            {
                if (!tabTargetEnemyList.Contains(enemy))
                {
                    RemoveFromTargetList(parent.GetComponentInChildren<Enemy>());
                }
            }
            
            cameraPlanes = GeometryUtility.CalculateFrustumPlanes(CameraManager.MyInstance.camMode == 0 ? Camera.main : Camera.main);
            
            // add all enemies hit to the list, assuming they are in the camera frame, alive, and not already in the list
            foreach (Collider col in tabTargetableEnemies)
            {
                parent = col.transform.parent.gameObject;
                
                if (GeometryUtility.TestPlanesAABB(cameraPlanes, col.bounds))
                {
                    if (!enemyList.Contains(parent.GetComponentInChildren<Enemy>()) && parent.GetComponentInChildren<Enemy>().IsAlive)
                    {
                        AddToTargetList(parent.GetComponentInChildren<Enemy>());
                    }
                }
                else
                {
                    if (enemyList.Contains(parent.GetComponentInChildren<Enemy>()))
                    {
                        RemoveFromTargetList(parent.GetComponentInChildren<Enemy>());
                    }
                }
            }
            
            if (enemyList.Count > 0)
            {
                enemyList[listIndex].Interact();
                
                listIndex++;
                
                if (listIndex >= enemyList.Count)
                {
                    listIndex = 0;
                }
                //Debug.Log("3Length: " + enemyList.Count + " | Index: " + listIndex);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Player.MyInstance.transform.position, tabTargetRange);
    }

    public void RemoveFromTargetList(Enemy enemy)
    {
        enemyList.Remove(enemy);
        
        if (listIndex >= enemyList.Count)
        {
            listIndex = enemyList.Count - 1;
            if (listIndex <= 0)
            {
                listIndex = 0;
            }
        }
    }
    
    public void AddToTargetList(Enemy enemy)
    {
        enemyList.Add(enemy);
        // TODO - there is a bug where if you only have a target group of one, then add one to the group, the focus does not swap with one click, but two
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
