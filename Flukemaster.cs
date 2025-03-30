using Modding;
using System;
using System.Collections.Generic;
using UnityEngine;
using Satchel;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

namespace Flukemaster {
    public class Flukemaster: Mod {
        new public string GetName() => "Flukemaster";
        public override string GetVersion() => "1.0.0.0";

        public static spellChoice activeSpell;

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            On.PlayMakerFSM.OnEnable += editFSM;
        }

        private void editFSM(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM self) {
            orig(self);
            if(self.FsmName == "Fireball Cast") {
                if(self.gameObject.name.Contains("Fireball Top") || self.gameObject.name.Contains("Fireball2 Top")) {
                    //create new events
                    try {
                        FsmEvent.AddFsmEvent(new FsmEvent("UP"));
                        FsmEvent.AddFsmEvent(new FsmEvent("DOWN WIDE"));
                        FsmEvent.AddFsmEvent(new FsmEvent("DOWN TALL"));
                    }
                    catch(Exception) { }
                    //add directional checks for up and down
                    FsmState lorr = self.GetValidState("L or R");
                    lorr.InsertAction(new chooseSpell(), 3);
                    //create new states
                    FsmState upState = self.CopyState("Cast Left", "Cast Up");
                    FsmState downWState = self.CopyState("Cast Left", "Cast Down Wide");
                    FsmState downTState = self.CopyState("Cast Left", "Cast Down Tall");
                    FsmState flukeUState = self.CopyState("Fluke L", "Fluke U");
                    FsmState flukeDWState = self.CopyState("Fluke L", "Fluke DW");
                    FsmState flukeDTState = self.CopyState("Fluke L", "Fluke DT");
                    FsmState finalUpState = self.CopyState("Flukes", "Flukes Up");
                    FsmState finalDownWideState = self.CopyState("Flukes", "Flukes Down Wide");
                    FsmState finalDownTallState = self.CopyState("Flukes", "Flukes Down Tall");
                    //connect transitions
                    lorr.AddTransition("UP", "Cast Up");
                    lorr.AddTransition("DOWN WIDE", "Cast Down Wide");
                    lorr.AddTransition("DOWN TALL", "Cast Down Tall");
                    upState.ChangeTransition("FLUKE", "Fluke U");
                    downWState.ChangeTransition("FLUKE", "Fluke DW");
                    downTState.ChangeTransition("FLUKE", "Fluke DT");
                    flukeUState.ChangeTransition("FINISHED", "Flukes Up");
                    flukeDWState.ChangeTransition("FINISHED", "Flukes Down Wide");
                    flukeDTState.ChangeTransition("FINISHED", "Flukes Down Tall");
                    //ignore dcrest
                    flukeUState.RemoveAction(1);
                    flukeDTState.RemoveAction(1);
                    flukeDWState.RemoveAction(1);
                    //set angle and count
                    bool isDarkSpell = self.gameObject.name.Contains("Fireball2");
                    finalUpState.AddAction(createNewFlingAction((FlingObjectsFromGlobalPool)finalUpState.Actions[0], isDarkSpell ? 43 : 23, 60, 120, 0, 16, 24));
                    finalUpState.Actions[0].Enabled = false;
                    finalDownTallState.AddAction(createNewFlingAction((FlingObjectsFromGlobalPool)finalDownTallState.Actions[0], 8, 80, 100, 1, 20, 25));
                    finalDownTallState.Actions[0].Enabled = false;
                    finalDownWideState.AddAction(createNewFlingAction((FlingObjectsFromGlobalPool)finalDownWideState.Actions[0], isDarkSpell ? 10 : 7, 160, 180, 0, 10, 18));
                    finalDownWideState.AddAction(createNewFlingAction((FlingObjectsFromGlobalPool)finalDownWideState.Actions[0], isDarkSpell ? 10 : 7, 0, 20, 0, 10, 18));
                    finalDownWideState.Actions[0].Enabled = false;
                }
            }
            else if(self.gameObject.name == "Knight" && self.FsmName == "Spell Control") {
                //define templates
                SpawnObjectFromGlobalPool spawnTop1 = self.GetValidState("Fireball 1").GetFirstActionOfType<SpawnObjectFromGlobalPool>();
                SpawnObjectFromGlobalPool spawnTop2 = self.GetValidState("Fireball 2").GetFirstActionOfType<SpawnObjectFromGlobalPool>();
                conditionalFlukeSpell.setDefaults(spawnTop1.gameObject, spawnTop2.gameObject, spawnTop1.spawnPoint, spawnTop1.storeObject);
                //vs+ss
                self.GetValidState("Level Check").InsertAction(new setSpellChoice(spellChoice.fireball), 0);
                //wraiths+shriek
                self.GetValidState("Level Check 3").InsertAction(new setSpellChoice(spellChoice.shriek), 0);
                replaceActionWithFlukes(self.GetValidState("Scream Burst 1"), 7, false);
                replaceActionWithFlukes(self.GetValidState("Scream Burst 2"), 8, true);
                //dive
                replaceActionWithFlukes(self.GetValidState("Quake1 Land"), 12, false, spellChoice.diveWide);
                replaceActionWithFlukes(self.GetValidState("Q2 Land"), 11, true, spellChoice.diveWide);
                replaceActionWithFlukes(self.GetValidState("Q2 Pillar"), 3, true, spellChoice.diveTall);
                
            }
        }

        private FsmStateAction createNewFlingAction(FlingObjectsFromGlobalPool source, int count, float angleMin, float angleMax, float originVariationX = 0, float speedMin = 14, float speedMax = 22) {
            FlingObjectsFromGlobalPool action = new();
            action.gameObject = source.gameObject;
            action.spawnPoint = source.spawnPoint;
            action.position.Value = source.position.Value;
            action.spawnMin = new FsmInt() { Value = count };
            action.spawnMax = new FsmInt() { Value = count };
            action.speedMin = new FsmFloat() { Value = speedMin };
            action.speedMax = new FsmFloat() { Value = speedMax };
            action.angleMin = new FsmFloat() { Value = angleMin };
            action.angleMax = new FsmFloat() { Value = angleMax };
            action.originVariationX = new FsmFloat() { Value = originVariationX };
            action.originVariationY = source.originVariationY;
            return action;
        }

        private void replaceActionWithFlukes(FsmState state, int action, bool isDark, spellChoice spell = spellChoice.fireball) {
            FsmOwnerDefault original = ((ActivateGameObject)state.Actions[action]).gameObject;
            state.InsertAction(new conditionalFlukeSpell(original, isDark), action + 1);
            state.RemoveAction(action);
            if(spell != spellChoice.fireball) {
                state.InsertAction(new setSpellChoice(spell), action);
            }
        }
    }

    public class chooseSpell: FsmStateAction {
        public override void OnEnter() {
            switch(Flukemaster.activeSpell) {
                case spellChoice.shriek:
                    Fsm.Event(FsmEvent.GetFsmEvent("UP"));
                    return;
                case spellChoice.diveWide:
                    Fsm.Event(FsmEvent.GetFsmEvent("DOWN WIDE"));
                    return;
                case spellChoice.diveTall:
                    Fsm.Event(FsmEvent.GetFsmEvent("DOWN TALL"));
                    return;
                case spellChoice.fireball:
                default:
                    Finish();
                    return;
            }
        }
    }

    public class setSpellChoice: FsmStateAction {
        spellChoice choice;
        public setSpellChoice(spellChoice choice) {
            this.choice = choice;
        }
        public override void OnEnter() {
            Flukemaster.activeSpell = choice;
            Finish();
        }
    }

    public class conditionalFlukeSpell: FsmStateAction {
        FsmOwnerDefault originalGameObject;
        FsmGameObject gameObject;
        static FsmGameObject whiteGameObject;
        static FsmGameObject blackGameObject;
        static FsmGameObject spawnPoint;
        static FsmGameObject storeObject;
        static FsmVector3 position;
        static FsmVector3 rotation;

        public conditionalFlukeSpell(FsmOwnerDefault originalGameObject, bool isDarkSpell) {
            this.originalGameObject = originalGameObject;
            gameObject = isDarkSpell ? blackGameObject : whiteGameObject;
        }

        public static void setDefaults(FsmGameObject whiteGameObject, FsmGameObject blackGameObject, FsmGameObject spawnPoint, FsmGameObject storeObject) {
            conditionalFlukeSpell.whiteGameObject = whiteGameObject;
            conditionalFlukeSpell.blackGameObject = blackGameObject;
            conditionalFlukeSpell.spawnPoint = spawnPoint;
            conditionalFlukeSpell.storeObject = storeObject;
            position = Vector3.zero;
            rotation = Vector3.zero;
        }

        public override void OnEnter() {
            if(PlayerData.instance.equippedCharm_11) {
                doSpawnMethod();
            }
            else {
                doActivateMethod();
            }
        }

        private void doActivateMethod() {
            GameObject ownerDefaultTarget = base.Fsm.GetOwnerDefaultTarget(originalGameObject);
            if(ownerDefaultTarget == null) {
                return;
            }
            ownerDefaultTarget.SetActive(true);
            base.Finish();
        }
            
        private void doSpawnMethod() {
            if(this.gameObject.Value != null) {
                Vector3 a = Vector3.zero;
                Vector3 euler = Vector3.up;
                if(spawnPoint.Value != null) {
                    a = spawnPoint.Value.transform.position;
                    if(!position.IsNone) {
                        a += position.Value;
                    }
                    euler = ((!rotation.IsNone) ? rotation.Value : spawnPoint.Value.transform.eulerAngles);
                }
                else {
                    if(!position.IsNone) {
                        a = position.Value;
                    }
                    if(!rotation.IsNone) {
                        euler = rotation.Value;
                    }
                }
                if(this.gameObject != null) {
                    GameObject value = this.gameObject.Value.Spawn(a, Quaternion.Euler(euler));
                    storeObject.Value = value;
                }
            }
            base.Finish();
        }
    }

    public enum spellChoice {
        fireball,
        diveWide,
        diveTall,
        shriek
    }
}