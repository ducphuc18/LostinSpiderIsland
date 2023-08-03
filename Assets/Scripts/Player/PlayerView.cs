using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerView : MonoBehaviour
{
    [Header("UI Ref")] 
    public Image _healthBarImg;
    public Image _expBarImg;
    public Text _txtLevel;
    
    [Header("Other Ref")]
    public GameObject _Camera;
    public Transform _playeModelview;
    public GameObject _particlePrefab;
    public Transform _weaponTransform;
    public List<ParticleSystem> _attackParticlesPrefabs;
    public ParticleSystem _changeCharacterEffectParticle;
    private Rigidbody _rigidbody;
    public event Action onAttackTarget;
    
    private bool _isOtherAimation;
    private bool _canMove;
    private bool _canAnimationMove;
    private bool _canNextAttack;
    private bool _canComboAttack;
    private bool _canUpdateRotation;
    private bool _canChangeCharacter;
    private bool _isOnComboAttack;
    private int _countCurrentStateAttack;
    private int _countCurrentStateParticleAttack;
    private int _currentTypeCharacter;
    private float _playerAngle;
    private float TransitionDuaration = 0.2f;
    public List<ParticleSystem> _attackParticles;
    public Animator _animatorPlayer { get; set; }
    public Transform _playerTransform { get; set; }
    public GameObject _swordCharacter { get; set; }
    public GameObject _gunCharacter{ get; set; }
    public PlayerInput _PlayerInput { get; set; }
    
    
  
    public void initialize()
    {
        _currentTypeCharacter = 2;
        _countCurrentStateAttack = 2;
        _countCurrentStateParticleAttack = 0;
        _isOtherAimation = false;
        _canChangeCharacter = true;
        _playerTransform = gameObject.GetComponent<Transform>();
        _rigidbody = gameObject.GetComponent<Rigidbody>();
        _PlayerInput = GetComponent<PlayerInput>();
        _healthBarImg = GameObject.FindWithTag("HealthBar").GetComponent<Image>();
        _expBarImg = GameObject.FindWithTag("ExpBar").GetComponent<Image>();
        _txtLevel = GameObject.FindWithTag("TxtLevel").GetComponent<Text>();
        _Camera = Camera.main.gameObject;
    }

    public void initializeModel(GameObject SwordModelPrefab, GameObject GunModelPrefab)
    {
         if (SwordModelPrefab != null)
             _swordCharacter = Instantiate(SwordModelPrefab, this.gameObject.transform);
         if (GunModelPrefab != null)
             _gunCharacter = Instantiate(GunModelPrefab, this.gameObject.transform);
        // First init
        
        _animatorPlayer = _swordCharacter.gameObject.GetComponentInChildren<Animator>();// lay thanh phan animator c?a nhân v?t c?m ki?m
        _playeModelview = _swordCharacter.transform;// set v? trí c?a nhân v?t c?m ki?m
        _gunCharacter.SetActive(false);

        foreach (var P in _attackParticlesPrefabs)// muc dich la spawn tat ca hieu ung chem va cho vao list _attackParticles
        {
            var CreatP = Instantiate(P, this.gameObject.transform);
            CreatP.Stop();
            _attackParticles.Add(CreatP);// hieu ung khi chem
        }
    }
    public void Move(Vector2 MoveValue, float _moveSpeed,bool BoostSpeed)
    {
        Vector3 _move = new Vector3(MoveValue.x, 0f, MoveValue.y).normalized;
        Quaternion _playerRotation = _Camera.transform.rotation; // lay giá tr? xoay theo camera
        _playerRotation.z = 0;
        _playerRotation.x = 0;
        Vector3 _moveDirection = _playerRotation * _move;
        _rigidbody.MovePosition(transform.position + _moveDirection * (_moveSpeed * Time.deltaTime));//***
        onMoveAnimation(MoveValue.x, MoveValue.y, BoostSpeed);
        if (_moveDirection != Vector3.zero )
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
            _playeModelview.transform.rotation = Quaternion.Slerp(_playeModelview.transform.rotation, //Quaternion.Slerp t?o hi?u ?ng xoay tr?n tru gi?a 2 góc
                targetRotation, 100 * Time.deltaTime);//***
            
            foreach (var p in _attackParticles) // cho hieu ung chem xoay theo player
            {
                var rotation = p.gameObject.transform.rotation;
                rotation = Quaternion.Euler(rotation.eulerAngles.x,_playeModelview.transform.eulerAngles.y,rotation.eulerAngles.z);
                p.gameObject.transform.rotation = rotation;//tao goc quay theo truc y
            }
        }
    }

    public void Attack()
    {
        onAttackTarget?.Invoke(); // khi nào kích ho?t s? ki?n này??
        if(_swordCharacter.activeSelf) // n?u dang dung player co ki?m
            Timing.RunCoroutine(AttackParticles());
    }
    IEnumerator<float> AttackParticles() // ch?y l?n l??t hi?u ?ng t?n công 
    {
        _attackParticles[_countCurrentStateParticleAttack].Play();
        yield return Timing.WaitForSeconds(0.1f);
        _attackParticles[_countCurrentStateParticleAttack].Stop();
        _countCurrentStateParticleAttack++;
    }
    public void ChangeAttackType()// ??i tr?ng thái tân công c?a nhân v?t
    {
        if (_canChangeCharacter)
        {
            _canChangeCharacter = false;
            switch (_currentTypeCharacter) // 2
            {
                case 1:
                    Timing.RunCoroutine(onChangeCharacter(_gunCharacter,_swordCharacter,2));
                    break;
                case 2:
                    Timing.RunCoroutine(onChangeCharacter(_swordCharacter,_gunCharacter,1));
                    break;
            }
        }
    }
    public void UpdateHealthBar(float _curhealth, float _maxHealth)
    {
        float slderPercenr = _curhealth / _maxHealth;  
        _healthBarImg.fillAmount = slderPercenr;
    }

    public void UpdateExpBar(float _curExp, float MaxExpLvUP, int _level)
    {
        float slderPercenr = _curExp / MaxExpLvUP;  
        _expBarImg.fillAmount = slderPercenr;
       _txtLevel.text = $"{_level}";
    }
    IEnumerator<float> onChangeCharacter(GameObject OldGO, GameObject NewGO, int changeState)
    {
        OldGO.SetActive(false);// t?t nhân v?t c?
        ShowParticle();// show ra hi?u ?ng chuy?n ??i
        _animatorPlayer = null;
        yield return Timing.WaitForSeconds(0.3f);
        NewGO.SetActive(true);
        NewGO.transform.position = OldGO.transform.position;
        NewGO.transform.rotation = OldGO.transform.rotation;
        _playeModelview = NewGO.transform;
        _animatorPlayer = NewGO.gameObject.GetComponentInChildren<Animator>();
        _currentTypeCharacter = changeState;
        _canChangeCharacter = true;
        onCompleComboAnimation();
    }

    public void ShowParticle()
    {
        if (_changeCharacterEffectParticle == null)// particle 
        {
            GameObject PTC = Instantiate(_particlePrefab, this.gameObject.transform);
            _changeCharacterEffectParticle = PTC.GetComponentInChildren<ParticleSystem>();
        }
        _changeCharacterEffectParticle.Play();
    }
    

    #region Animation Manage
    public void onAttackAnimation(PlayerAttackType _type)
    {
        _isOtherAimation = true;
        switch (_type)
        {
            case PlayerAttackType.Melee:
                _animatorPlayer.CrossFade(AnimationString.Attack1.ToString(),TransitionDuaration,0);
                break;
            case PlayerAttackType.ComboAttack:
                _canComboAttack = true;
                // Start ComboAttack only one
                if (!_isOnComboAttack)
                {
                    _animatorPlayer.CrossFade(AnimationString.Attack1.ToString(), TransitionDuaration, 0);
                    _isOnComboAttack = true;
                }
                break;
        }
    }
    
    public void onMoveAnimation( float Horizontal,float Vertical, bool BoostSpeed) // t?o animator
    {
        if(Horizontal !=0 || Vertical !=0)
        {
            if (!_isOtherAimation && _canAnimationMove )
            {
                if(BoostSpeed)
                    _animatorPlayer.CrossFade(AnimationString.Run.ToString(), TransitionDuaration, 0);
                else
                    _animatorPlayer.CrossFade(AnimationString.Walk.ToString(), TransitionDuaration, 0);
                _canAnimationMove = false;
            }
        }else if (Horizontal == 0 || Vertical == 0 )
        {
            if (!_isOtherAimation && !_canAnimationMove)
            {
                _animatorPlayer.CrossFade(AnimationString.Idle.ToString(), TransitionDuaration, 0);
                _canAnimationMove = true;
            }
        }

    }

    #region Event Animation
    public void onComboAttackAnimation()//chay het combo tan cong
    {
        string StringComboAttack = "Attack" + _countCurrentStateAttack.ToString();
        if (_canComboAttack && _countCurrentStateAttack < 5)
        {
            _isOnComboAttack = true;
            _animatorPlayer.CrossFade(StringComboAttack,TransitionDuaration,0);
            _canComboAttack = false;
            _countCurrentStateAttack++;
        }
        else
        {
            //if no double click reset attack combo.
            onCompleComboAnimation();
        }
    }
    public void onCompleComboAnimation()// neu da chay het combo tan cong trong mang
    {
        if (_countCurrentStateParticleAttack != 0)
            _countCurrentStateParticleAttack = 0;
        _isOnComboAttack = false;
        _countCurrentStateAttack = 2;
        foreach (var p in _attackParticles)
        {
            p.Stop();
        }
        onCompleteanimation();
    }
    public void onCompleteanimation()
    {
        _isOtherAimation = false;
        if(!_isOnComboAttack)
            _animatorPlayer.CrossFade(AnimationString.Idle.ToString(),TransitionDuaration,0);
    }

  

    #endregion
    
    #endregion

   
}
