using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class EnemyView : MonoBehaviour
{
    public NavMeshAgent agent;
    public Animator amin;
    public EnemyDataSO enemyDataSO;
    public Transform _curEnemyPos;
    public HeathBar healhBar;
    public TakeDamage _takeDamage;
    public DefenseMode defenseMode;
    public Transform attackPos;
    public float range;
    public LayerMask playerLayer;
    public bool _isDie { get; set; }
    
    private bool _canSpawn;
    private float _defenseIndex = 0.4f;
    private bool _isHealed;
    private float _curHealh;
    private bool _isAttack;


    public float CurHealh { get => _curHealh;  }
    public bool IsAttack { get => _isAttack; set => _isAttack = value; }

    public void Initilize()
    {
        agent = GetComponent<NavMeshAgent>();
        _curHealh = EnemyManager.instance.CurEnemyHealth;
        if (agent == null)
        {
            Destroy(gameObject); 
            return;
        }
        StartCoroutine(CheckAttackRange());
    }
    
    public void Move()
    {
       
        if (agent!= null && EnemyManager.instance.playerPos.transform.position != null)
        {
          agent.SetDestination(EnemyManager.instance.playerPos.transform.position);
        }    
        else if(agent == null || !agent.pathPending && agent.path.status == NavMeshPathStatus.PathInvalid)
        {
            Destroy(gameObject);
        }    
        agent.speed = EnemyManager.instance.EnemySpeed;
        amin.SetBool(EnemyAmin.Attack.ToString(), false);
        amin.SetBool(EnemyAmin.Run.ToString(), true);
        amin.SetBool(EnemyAmin.Hit.ToString(), false);
    }
  
    public IEnumerator CheckAttackRange()
    {
        while(true && healhBar._healthBarImg.fillAmount > 0)
        {
            Collider[] hit = Physics.OverlapSphere(attackPos.position, range, playerLayer);

            foreach (var attack in hit)
            {
               

                if (attack != null && !_isAttack)
                {
                    IDamage damage = attack.GetComponent<IDamage>();
                    damage?.getHit(EnemyManager.instance.EnemyAttack);
                    _isAttack = true;
                }

                yield return new WaitForSeconds(0.3f);
                _isAttack = false;
            }
            yield return null;  
        }
    }    


    public void initTakeDamage()
    {
        _isDie = false;
        _takeDamage = gameObject.GetComponent<TakeDamage>();
        if(_takeDamage != null)
            InitAction();
    }
    private void InitAction()
    {
        _takeDamage.OnDamage += getHit;
        _takeDamage.GetHPFunc += getHP;
    }
    private void getHit(float value)
    {
        
        healhBar.HandEnemyhealth((int)value, this.gameObject.GetComponent<EnemyView>());
        healhBar.UpdateHealthBar();
        if (healhBar._healthBarImg.fillAmount <= 0)
        {
            _isDie = true;
            if (!_canSpawn)
            {
                EnemyManager.instance.Index--;
                EnemyManager.instance.EnemyEnough = false;
                StartCoroutine(EnemyManager.instance.CreatEnemy());
                _canSpawn = true;
            }
            
            Destroy(gameObject, 1.5f);
            
        }
        else if(healhBar._healthBarImg.fillAmount > 0)
        {
            DamePopupGenerator._instance.createPopUp(this.gameObject.transform,value,Color.red);
            _canSpawn = false;
            if(EnemyManager.instance.CurEnemyHealth <= 0.5f && !_isHealed)
            {
                EnemyManager.instance.CurEnemyHealth += (int)_defenseIndex;
                StartCoroutine(defenseMode.EnemyDefenseloop());
                _isHealed = true;
            }    
        }    
       
    }
    private float getHP()
    {
        if(_isDie)
            gameObject.GetComponent<Collider>().enabled = false;
        return healhBar._healthBarImg.fillAmount;
    }
    public void HitToIdle()
    {
        amin.SetBool(EnemyAmin.Hit.ToString(), false);
    }
  
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(attackPos.position, range);
    }


}
