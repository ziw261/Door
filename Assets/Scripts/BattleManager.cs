﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class BattleManager : MonoBehaviour {

    public static BattleManager instance;

    private bool battleActive;

    public GameObject battleScene;

    public Transform[] playerPositions;
    public Transform[] enemyPositions;

    public BattleChar[] playerPrefabs;
    public BattleChar[] enemyPrefabs;

    public List<BattleChar> activeBattlers = new List<BattleChar>();

    public int currentTurn;
    public bool turnWaiting;
    public GameObject uiButtonsHolder;

    public BattleMove[] movesList;
    public GameObject enemyAttackEffect;

    public DamageNumber theDamageNumber;

    public Text[] playerName, playerHP, playerMP;

    public GameObject targetMenu;
    public BattleTargetButton[] targetButtons;

    public GameObject magicMenu;
    public BattleMagicSelect[] magicButtons;

    public BattleNotification battleNotice;

    public int chanceToFlee = 35;

    private bool fleeing;

    public string gameOverScene;

    public int rewardXP;
    public string[] rewardItems;

    public bool cannotFlee;

    private string skipTurn = "?";
    
    // Start is called before the first frame update
    void Start() {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update(){
        /*
        if(Input.GetKeyDown(KeyCode.T)) {
            BattleStart(new string[] { "Eyeball", "Spider", "Skeleton" }, false);
        }
        */

        if (battleActive) {
            
            if (turnWaiting) {
                if (activeBattlers[currentTurn].isPlayer) {
                    uiButtonsHolder.SetActive(true);
                } else {
                    uiButtonsHolder.SetActive(false);
                    
                    // enemy should attack
                    StartCoroutine(EnemyMoveCo());
                }
            }
            
            /*
            if (Input.GetKeyDown(KeyCode.N)) {
                NextTurn();
            }
            */
            
        }
        
        
    }

    public void BattleStart(string[] enemiesToSpawn, bool setCannotFlee) {
        if(!battleActive) {
            battleActive = true;

            cannotFlee = setCannotFlee;
            
            GameManager.instance.battleActive = true;

            transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);

            battleScene.SetActive(true);


            //Switch battle music
            AudioManager.instance.PlayBGM(8);

            checkMagicCai();
            checkMagicMu();
            checkMagicShi();
            for(int i=0; i<playerPositions.Length; i++) {
                if(GameManager.instance.playerStats[i].gameObject.activeInHierarchy) {
                    for(int j=0; j<playerPrefabs.Length; j++) {
                        if(playerPrefabs[j].charName == GameManager.instance.playerStats[i].charName) {
                            BattleChar newPlayer = Instantiate(playerPrefabs[j], playerPositions[i].position, playerPositions[i].rotation);

                            // move pos instead of move player
                            newPlayer.transform.parent = playerPositions[i];
    
                            activeBattlers.Add(newPlayer);


                            CharStats thePlayer = GameManager.instance.playerStats[i];
                            activeBattlers[i].currentHP = thePlayer.currentHP;
                            activeBattlers[i].maxHP = thePlayer.maxHP;
                            activeBattlers[i].currentMP = thePlayer.currentMP;
                            activeBattlers[i].maxMP = thePlayer.maxMP;
                            activeBattlers[i].strength = thePlayer.strength;
                            activeBattlers[i].defence = thePlayer.defence;
                            activeBattlers[i].wpnPower = thePlayer.wpnPwr;
                            activeBattlers[i].armrPower = thePlayer.armrPwr;
                        }
                    }


                }
            }


            for(int i=0; i<enemiesToSpawn.Length; i++) {
                if(enemiesToSpawn[i] != "") {
                    for (int j = 0; j < enemyPrefabs.Length; j++) {

                        if (enemyPrefabs[j].charName == enemiesToSpawn[i]) {
                            BattleChar newEnemy = Instantiate(enemyPrefabs[j], enemyPositions[i].position, enemyPositions[i].rotation);
                            newEnemy.transform.parent = enemyPositions[i];
                            activeBattlers.Add(newEnemy);
                        }
                    }
                }
            }
            
            

            turnWaiting = true;
            currentTurn = Random.Range(0, activeBattlers.Count);
            
            UpdateUIStats();
        }
    }

    public void NextTurn() {
        currentTurn++;
        if (currentTurn >= activeBattlers.Count) {
            currentTurn = 0;
        }
        
        turnWaiting = true;
        
        UpdateBattle();
        UpdateUIStats();
        
        if (skipTurn == activeBattlers[currentTurn].charName) {
            skipTurn = "?";
            currentTurn++;
            if (currentTurn >= activeBattlers.Count) {
                currentTurn = 0;
            }
        
            turnWaiting = true;
        
            UpdateBattle();
            UpdateUIStats();
        }

       
    }

    public void UpdateBattle() {
        bool allEnemiesDead = true;
        bool allPlayersDead = true;

        for (int i = 0; i < activeBattlers.Count; i++) {
            if (activeBattlers[i].currentHP < 0) {
                activeBattlers[i].currentHP = 0;
            }

            if (activeBattlers[i].currentHP == 0) {
                // Handle dead battler

                if (activeBattlers[i].isPlayer) {
                    activeBattlers[i].theSprite.sprite = activeBattlers[i].deadSprite;
                } else {
                    activeBattlers[i].EnemyFade();
                }
                
            } else {
                
                if (activeBattlers[i].isPlayer) {
                    allPlayersDead = false;
                    activeBattlers[i].theSprite.sprite = activeBattlers[i].aliveSprite;
                } else {
                    allEnemiesDead = false;
                }
            }
        }

        if (allEnemiesDead || allPlayersDead) {
            if (allEnemiesDead) {
                // end battle in victory
                StartCoroutine(EndBattleCo());
            } else {
                // end battle in failure
                StartCoroutine(GameOverCo());
            }
            
            /*
            battleScene.SetActive(false);
            GameManager.instance.battleActive = false;
            battleActive = false;
            */
        } else {
            while (activeBattlers[currentTurn].currentHP == 0) {
                currentTurn++;
                if (currentTurn >= activeBattlers.Count) {
                    currentTurn = 0;
                }
            }
        }
    }

    public IEnumerator EnemyMoveCo() {
        turnWaiting = false;
        yield return new WaitForSeconds(1f);
        EnemyAttack();
        yield return new WaitForSeconds(1f);
        NextTurn();
    }
    
    
    public void EnemyAttack() {
        List<int> players = new List<int>();
        for (int i = 0; i < activeBattlers.Count; i++) {
            if (activeBattlers[i].isPlayer && activeBattlers[i].currentHP > 0) {
                players.Add(i);
            }
        }
        
        // attack a random player
        int selectedTarget = players[Random.Range(0, players.Count)];

       // activeBattlers[selectedTarget].currentHP -= 20;

       int selectAttack = Random.Range(0, activeBattlers[currentTurn].movesAvailable.Length);
       int movePower = 0;
       for (int i = 0; i < movesList.Length; i++) {
           
           // Check if enemy holds the ability
           if (movesList[i].moveName == activeBattlers[currentTurn].movesAvailable[selectAttack]) {
               
               // istantiate the effect on target
               Instantiate(movesList[i].theEffect, activeBattlers[selectedTarget].transform.position,
                   activeBattlers[selectedTarget].transform.rotation);

               movePower = movesList[i].movePower;
           }
       }

       Instantiate(enemyAttackEffect, activeBattlers[currentTurn].transform.position,
           activeBattlers[currentTurn].transform.rotation);
       
       DealDamage(selectedTarget,movePower);
    }


    public void DealDamage(int target, int movePower) {
        float atkPwr = activeBattlers[currentTurn].strength + activeBattlers[currentTurn].wpnPower;
        float defPwr = activeBattlers[target].defence + activeBattlers[target].armrPower;

        float damageCalc = (atkPwr/defPwr) * movePower * Random.Range(0.9f, 1.1f);
        int damageToGive = Mathf.RoundToInt(damageCalc);
        
        Debug.Log(activeBattlers[currentTurn].charName + " is dealing " + damageCalc + "(" + damageToGive + ") damge to " + activeBattlers[target].charName);

        activeBattlers[target].currentHP -= damageToGive;
        
        Instantiate(theDamageNumber, activeBattlers[target].transform.position, activeBattlers[target].transform.rotation).SetDamage(damageToGive);
        
        UpdateUIStats();
    }


    public void UpdateUIStats() {
        for (int i = 0; i < playerName.Length; i++) {
            if (activeBattlers.Count > i) {
                if (activeBattlers[i].isPlayer) {
                    BattleChar playerData = activeBattlers[i];
                    
                    playerName[i].gameObject.SetActive(true);
                    playerName[i].text = playerData.charName;
                    playerHP[i].text = Mathf.Clamp(playerData.currentHP,0,int.MaxValue) + "/" + playerData.maxHP;
                    playerMP[i].text = Mathf.Clamp(playerData.currentMP,0,int.MaxValue) + "/" + playerData.maxMP;
                } else {
                    playerName[i].gameObject.SetActive(false);
                }
            } else {
                playerName[i].gameObject.SetActive(false);
            }
        }
        
    }

    public void PlayerAttack(string moveName, int selectedTarget) {

        
        int movePower = 0;
        
        
        for (int i = 0; i < movesList.Length; i++) {
           
            // Check if enemy holds the ability
            if (movesList[i].moveName == moveName) {

               
                    // istantiate the effect on target
                Instantiate(movesList[i].theEffect, activeBattlers[selectedTarget].transform.position,
                    activeBattlers[selectedTarget].transform.rotation);
                

                movePower = movesList[i].movePower;
            }
        }
        
        Instantiate(enemyAttackEffect, activeBattlers[currentTurn].transform.position,
            activeBattlers[currentTurn].transform.rotation);

        Debug.Log(movePower);

        if (movePower == -1) {
            //DealDamage(selectedTarget,movePower);
            //Debug.Log("Been here, updated skipturn");
            skipTurn = activeBattlers[selectedTarget].charName;
        } else if (movePower == -2) {
            int temp = activeBattlers[currentTurn].maxHP / 2;
            activeBattlers[currentTurn].currentHP += temp;
            if (activeBattlers[currentTurn].currentHP > activeBattlers[currentTurn].maxHP) {
                activeBattlers[currentTurn].currentHP = activeBattlers[currentTurn].maxHP;
            }
            DealDamage(selectedTarget, 15);
            //UpdateUIStats();
        } else {
            
            DealDamage(selectedTarget, movePower);
        }

        uiButtonsHolder.SetActive(false);
        targetMenu.SetActive(false);
        
        NextTurn();
    }

    public void OpenTargetMenu(string moveName) {
        targetMenu.SetActive(true);

        List<int> Enemies = new List<int>();

        for (int i = 0; i < activeBattlers.Count; i++) {
            if (!activeBattlers[i].isPlayer) {
                Enemies.Add(i);
            }
        }

        for (int i = 0; i < targetButtons.Length; i++) {
            if (Enemies.Count > i && activeBattlers[Enemies[i]].currentHP > 0) {
                targetButtons[i].gameObject.SetActive(true);
                targetButtons[i].moveName = moveName;
                targetButtons[i].activeBattlerTarget = Enemies[i];
                targetButtons[i].targetName.text = activeBattlers[Enemies[i]].charName;

            } else {
                targetButtons[i].gameObject.SetActive(false);

            }
        }
    }

    public void OpenMagicMenu() {
        magicMenu.SetActive(true);

        for (int i = 0; i < magicButtons.Length; i++) {
            if (activeBattlers[currentTurn].movesAvailable.Length > i) {
                magicButtons[i].gameObject.SetActive(true);
                magicButtons[i].spellName = activeBattlers[currentTurn].movesAvailable[i];
                magicButtons[i].nameText.text = magicButtons[i].spellName;

                for (int j = 0; j < movesList.Length; j++) {
                    if (movesList[j].moveName == magicButtons[i].spellName) {
                        magicButtons[i].spellCost = movesList[j].moveCost;
                        magicButtons[i].costText.text = magicButtons[i].spellCost.ToString();
                    }
                }
                
            } else {
                magicButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void Flee() {
        if (cannotFlee) {
            battleNotice.theText.text = "You can not flee!";
            battleNotice.Activate();
        } else {
            int fleeSuccess = Random.Range(0, 100);
            if (fleeSuccess < chanceToFlee) {
                //end the battle
                //battleActive = false;
                //battleScene.SetActive(false);
                fleeing = true;
                StartCoroutine(EndBattleCo());
            } else {
                NextTurn();
                battleNotice.theText.text = "Couldn't escape";
                battleNotice.Activate();
            }
        }

    }

    public IEnumerator EndBattleCo() {
        battleActive = false;
        uiButtonsHolder.SetActive(false);
        targetMenu.SetActive(false);
        magicMenu.SetActive(false);
        
        yield return new WaitForSeconds(.5f);
        UIFade.instance.FadeToBlack();
        
        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < activeBattlers.Count; i++) {
            if (activeBattlers[i].isPlayer) {
                for (int j = 0; j < GameManager.instance.playerStats.Length; j++) {
                    if (activeBattlers[i].charName == GameManager.instance.playerStats[j].charName) {
                        GameManager.instance.playerStats[j].currentHP = activeBattlers[i].currentHP;
                        GameManager.instance.playerStats[j].currentMP = activeBattlers[i].currentMP;
                    }
                }
            }
            
            Destroy(activeBattlers[i].gameObject);
        }
        
        UIFade.instance.FadeFromBlack();
        battleScene.SetActive(false);
        activeBattlers.Clear();
        currentTurn = 0;
        //GameManager.instance.battleActive = false;
        if (fleeing) {
            GameManager.instance.battleActive = false;
           

            fleeing = false;
        } else {
            BattleReward.instance.OpenRewardScreen(rewardXP, rewardItems);
        }

      
        
        AudioManager.instance.PlayBGM(FindObjectOfType<CameraController>().musicToPlay);

    }

    public IEnumerator GameOverCo() {

        battleActive = false;
        UIFade.instance.FadeToBlack();
        yield return new WaitForSeconds(1.5f);
        battleScene.SetActive(false);
        SceneManager.LoadScene(gameOverScene);
    }

    public void checkMagicCai() {
        for (int i = 0; i < GameManager.instance.itemsHeld.Length; i++) {
            if (GameManager.instance.itemsHeld[i] == "Magic_Cai") {
                for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
                    if (playerPrefabs[0].movesAvailable[j] == " ") {
                   
                        playerPrefabs[0].movesAvailable[j] = "闭";
                        return;
                    } else if (playerPrefabs[0].movesAvailable[j] == "闭") {
                        playerPrefabs[0].movesAvailable[j] = "闭";
                        //Debug.Log("Been here successfully");
                        return;
                    }
                    
                    
                }
                
            } 
        }
        
        for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
            if (playerPrefabs[0].movesAvailable[j] == "闭") {
                playerPrefabs[0].movesAvailable[j] = " ";
                //Debug.Log("Been here successfully1");
                return;
            }
        }
    }
    
    
    public void checkMagicMu() {
        for (int i = 0; i < GameManager.instance.itemsHeld.Length; i++) {
            if (GameManager.instance.itemsHeld[i] == "Magic_Mu") {
                for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
                    if (playerPrefabs[0].movesAvailable[j] == " ") {
                   
                        playerPrefabs[0].movesAvailable[j] = "闲";
                        return;
                    } else if (playerPrefabs[0].movesAvailable[j] == "闲") {
                        playerPrefabs[0].movesAvailable[j] = "闲";
                        //Debug.Log("Been here successfully");
                        return;
                    }
                    
                    
                }
                
            } 
        }
        
        for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
            if (playerPrefabs[0].movesAvailable[j] == "闲") {
                playerPrefabs[0].movesAvailable[j] = " ";
                //Debug.Log("Been here successfully1");
                return;
            }
        }
    }
    
    public void checkMagicShi() {
        for (int i = 0; i < GameManager.instance.itemsHeld.Length; i++) {
            if (GameManager.instance.itemsHeld[i] == "Magic_Shi") {
                for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
                    if (playerPrefabs[0].movesAvailable[j] == " ") {
                   
                        playerPrefabs[0].movesAvailable[j] = "闹";
                        return;
                    } else if (playerPrefabs[0].movesAvailable[j] == "闹") {
                        playerPrefabs[0].movesAvailable[j] = "闹";
                        //Debug.Log("Been here successfully");
                        return;
                    }
                    
                    
                }
                
            } 
        }
        
        for (int j = 0; j < playerPrefabs[0].movesAvailable.Length; j++) {
            if (playerPrefabs[0].movesAvailable[j] == "闹") {
                playerPrefabs[0].movesAvailable[j] = " ";
                //Debug.Log("Been here successfully1");
                return;
            }
        }
    }
    
    
    
}
