using RimWorld;
using Harmony;
using System.Reflection;
using Verse;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace removeAfterDeath
{
    [StaticConstructorOnStartup]
    static class HarmonyPatch_One
    {
        static HarmonyPatch_One()
        {
            HarmonyInstance harmony = HarmonyInstance.Create("myHarmonyTest");

            

            //MethodInfo targetMethod = AccessTools.Method(AccessTools.TypeByName("MedicalRecipesUtility"), "IsCleanAndDroppable");
            //HarmonyMethod Patch_IsCleanAndDroppable_Prefix = new HarmonyMethod(typeof(Patch_MedRec).GetMethod("IsCleanAndDroppable_Prefix"));
            
            MethodInfo targetMethod2 = AccessTools.Method(AccessTools.TypeByName("Recipe_RemoveBodyPart"), "ApplyOnPawn");
            HarmonyMethod Patch_ApplyOnPawn_Prefix = new HarmonyMethod(typeof(Patch_Recipe_RemoveBodyPart).GetMethod("ApplyOnPawn_Prefix"));
            //harmony.Patch(targetMethod2, Patch_ApplyOnPawn_Prefix, null);

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        
    }

    //[HarmonyPatch(typeof(ITab_Pawn_Health),"FillTab")]
    //static class Patch_FillTab
    //{
    //    static readonly FieldInfo p = AccessTools.Field(typeof(ITab_Pawn_Health), "PawnForHealth");
        
    //    [HarmonyPrefix]
    //    public static bool FillTab_Prefix(ITab_Pawn_Health __instance)
    //    {
    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(HealthCardUtility),"DrawPawnHealthCard",new Type[] { typeof(Rect),typeof(Pawn),typeof(bool),typeof(bool),typeof(Thing)})]
    static class Patch_HealthCard_Draw
    {
        
        [HarmonyPrefix]
        private static bool Method_Prefix(Rect outRect,Pawn pawn, bool allowOperations, bool showBloodLoss, Thing thingForMedBills)
        {
            if (pawn.Dead)
            {
                allowOperations = true;
               outRect = outRect.Rounded();
                Rect rect = new Rect(outRect.x, outRect.y, outRect.width * 0.375f, outRect.height).Rounded();
                Rect rect2 = new Rect(rect.xMax, outRect.y, outRect.width - rect.width, outRect.height);
                rect.yMin += 11f;
                HealthCardUtility.DrawHealthSummary(rect, pawn, allowOperations, thingForMedBills);
                HealthCardUtility.DrawHediffListing(rect2.ContractedBy(10f), pawn, showBloodLoss);
                return false;
            }
            else return true;
        }

        
    }

    [HarmonyPatch(typeof(HealthCardUtility),"DrawHealthSummary", new Type[] { typeof(Rect),typeof(Pawn),typeof(bool),typeof(Thing)})]
    static class Patch_DrawHealthSummary
    {
        //static Traverse onOTab = Traverse.Create<bool>();
        
        static readonly FieldInfo p = AccessTools.Field(typeof(HealthCardUtility), "onOperationTab");

        [HarmonyPrefix]
        private static bool Patch_DrawHealthSummary_Prefix(Rect rect, Pawn pawn, bool allowOperations, Thing thingForMedBills)
        {
            if (pawn.Dead)
            {
                Corpse corpse = (thingForMedBills as Corpse);
                //Log.Message("Own DrawHealthSummary "+allowOperations);

                GUI.color = Color.white;
                if (!allowOperations)
                {
                    p.SetValue(null,false);
                }
                Widgets.DrawMenuSection(rect);
                List<TabRecord> list = new List<TabRecord>();
                list.Add(new TabRecord("HealthOverview".Translate(), delegate
                {
                    p.SetValue(null, false);
                }, !(bool)p.GetValue(null)));
                if (allowOperations)
                {
                    string label = (!corpse.InnerPawn.RaceProps.IsMechanoid) ? "MedicalOperationsShort".Translate(new object[]
                    {
                         corpse.BillStack.Count
                    }) : "MedicalOperationsMechanoidsShort".Translate(new object[]
                    {
                         corpse.BillStack.Count
                    });
                    list.Add(new TabRecord(label, delegate
                    {
                        p.SetValue(null, true);
                    }, (bool)p.GetValue(null)));
                }
                TabDrawer.DrawTabs(rect, list);
                rect = rect.ContractedBy(9f);
                GUI.BeginGroup(rect);
                float curY = 0f;
                Text.Font = GameFont.Medium;
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperCenter;
                if ((bool)p.GetValue(null))
                {
                    //Log.Message("A");
                    PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.MedicalOperations, KnowledgeAmount.FrameDisplayed);
                    curY = (float)AccessTools.Method(typeof(HealthCardUtility), "DrawMedOperationsTab", new Type[] { typeof(Rect), typeof(Pawn), typeof(Thing), typeof(float) }).Invoke(null, new object[] {rect,pawn,thingForMedBills,curY }); //(rect, pawn, thingForMedBills, curY);
                }
                else
                {
                    //Log.Message("B");
                    curY = (float)AccessTools.Method(typeof(HealthCardUtility),"DrawOverviewTab",new Type[] {typeof(Rect), typeof(Pawn), typeof(float) }).Invoke(null, new object[] { rect, pawn, curY });
            }
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.EndGroup();

                return false;
            }
            else
            {
                //Log.Message("DrawHealthSummary forwarding");
                return true;
            }
        }
    }

    

    [HarmonyPatch(typeof(HealthCardUtility),"GenerateSurgeryOption", new Type[] { typeof(Pawn),typeof(Thing),typeof(RecipeDef),typeof(IEnumerable<ThingDef>),typeof(BodyPartRecord)})]
    
    public static class Patch_HealthCard_GenSur
    {
        [HarmonyPrefix]
        private static bool Method_Prefix_2(ref FloatMenuOption __result, Pawn pawn, Thing thingForMedBills, RecipeDef recipe, IEnumerable<ThingDef> missingIngredients, BodyPartRecord part = null)
        {
            //Traverse ThingDef_Traverse = Traverse.Create<ThingDef>();
            
            if (pawn.Dead)
            {
                //Log.Message("Own Generating SurgeryOptions");
                string text = recipe.Worker.GetLabelWhenUsedOn(pawn, part);
                if (part != null && !recipe.hideBodyPartNames)
                {
                    text = text + " (" + part.def.label + ")";
                }
                FloatMenuOption floatMenuOption;
                if (missingIngredients.Any())
                {
                    text += " (";
                    bool flag = true;
                    foreach (ThingDef missingIngredient in missingIngredients)
                    {
                        if (!flag)
                        {
                            text += ", ";
                        }
                        flag = false;
                        text += "MissingMedicalBillIngredient".Translate(missingIngredient.label);
                    }
                    text += ")";
                    floatMenuOption = new FloatMenuOption(text, null, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                else
                {
                    Action action = delegate
                    {
                        //Log.Message("Delegate action");
                        Corpse pawn2 = thingForMedBills as Corpse;
                        if (pawn2 != null)
                        {
                            Bill_Medical bill_Medical = new Bill_Medical(recipe);
                            //Log.Message("adding bill_Medical to billstack " + bill_Medical.ToString());
                            pawn2.BillStack.AddBill(bill_Medical);
                            bill_Medical.Part = part;
                            if (recipe.conceptLearned != null)
                            {
                                PlayerKnowledgeDatabase.KnowledgeDemonstrated(recipe.conceptLearned, KnowledgeAmount.Total);
                            }
                            Map map = thingForMedBills.Map;
                            if (!map.mapPawns.FreeColonists.Any((Pawn col) => recipe.PawnSatisfiesSkillRequirements(col)))
                            {
                                Bill.CreateNoPawnsWithSkillDialog(recipe);
                            }
                            if (!pawn2.InnerPawn.InBed() && pawn2.InnerPawn.RaceProps.IsFlesh)
                            {
                                //if (pawn2.InnerPawn.RaceProps.Humanlike)
                                //{
                                //    if (!map.listerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed && RestUtility.CanUseBedEver(pawn, x.def) && ((Building_Bed)x).Medical))
                                //    {
                                //        Messages.Message("MessageNoMedicalBeds".Translate(), pawn2, MessageTypeDefOf.CautionInput);
                                //    }
                                //}
                                //else if (!map.listerBuildings.allBuildingsColonist.Any((Building x) => x is Building_Bed && RestUtility.CanUseBedEver(pawn, x.def)))
                                //{
                                //    Messages.Message("MessageNoAnimalBeds".Translate(), pawn2, MessageTypeDefOf.CautionInput);
                                //}
                            }
                            if (pawn2.Faction != null && !pawn2.Faction.def.hidden && !pawn2.Faction.HostileTo(Faction.OfPlayer) && recipe.Worker.IsViolationOnPawn(pawn2.InnerPawn, part, Faction.OfPlayer))
                            {
                                Messages.Message("MessageMedicalOperationWillAngerFaction".Translate(pawn2.Faction), pawn2, MessageTypeDefOf.CautionInput);
                            }
                            //Log.Message("Log10");
                            ThingDef minRequiredMedicine =  (ThingDef)AccessTools.Method(typeof(HealthCardUtility), "GetMinRequiredMedicine", new Type[] { typeof(RecipeDef)}).Invoke(null, new object[] { recipe }); //(rect, pawn, thingForMedBills, curY);
                            //Log.Message("Log11");
                            if (minRequiredMedicine != null && pawn2.InnerPawn.playerSettings != null && !pawn2.InnerPawn.playerSettings.medCare.AllowsMedicine(minRequiredMedicine))
                            {
                                Messages.Message("MessageTooLowMedCare".Translate(minRequiredMedicine.label, pawn2.LabelShort, pawn2.InnerPawn.playerSettings.medCare.GetLabel()), pawn2, MessageTypeDefOf.CautionInput);
                            }
                        }
                    };
                    floatMenuOption = new FloatMenuOption(text, action, MenuOptionPriority.Default, null, null, 0f, null, null);
                }
                floatMenuOption.extraPartWidth = 29f;
                floatMenuOption.extraPartOnGUI = ((Rect rect) => Widgets.InfoCardButton(rect.x + 5f, rect.y + (rect.height - 24f) / 2f, recipe));
                __result = floatMenuOption;


                return false;
            }
            else
            {
                //Log.Message("Generating SurgeryOptions forwarding");
                return true;
            }   
         }


    }

    static class Patch_MedRec
    {
        public static bool IsCleanAndDroppable_Prefix(Pawn pawn,BodyPartRecord part, ref bool __result)
        {
            //Log.Message("IsCleanAndDroppable " + part.ToString());
            if (pawn.Dead)
            {
                __result = part.def.spawnThingOnRemoved != null && (bool)AccessTools.Method(AccessTools.TypeByName("MedicalRecipesUtility"), "IsClean", new Type[] { typeof(Pawn), typeof(BodyPartRecord) }).Invoke(null, new object[] { pawn, part });
                return false;
            } else
                return true;
        }
    }

    static class Patch_Recipe_RemoveBodyPart
    {
        public static bool ApplyOnPawn_Prefix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            //Log.Message("ApplyOnPawn");
            //Log.Message(pawn.ToString());
            //Log.Message(part.ToString());
            //Log.Message(billDoer.ToString());
            //Log.Message(ingredients.ToString());
            //Log.Message(bill.ToString());
            //DamageInfo i = new DamageInfo(DamageDefOf.SurgicalCut, 99999, -1f, null, part, null, DamageInfo.SourceCategory.ThingOrUnknown);
            //Log.Message(i.ToString());
            return true;
        }
    }

    [HarmonyPatch(typeof(Thing), "TakeDamage", new Type[] { typeof(DamageInfo)})]
    static class Patch_Thing
    {
        [HarmonyPrefix]
        public static bool TakeDamage_Prefix(Thing __instance, DamageInfo dinfo, ref DamageWorker.DamageResult __result)
        {
            if (__instance as Pawn == null)
            {

                return true;

            }
            else
            {
                //Log.Message("Own TakeDamage");
                //Log.Message("Log1");
                if (__instance.Destroyed)
                {
                    __result = DamageWorker.DamageResult.MakeNew();
                }
                //Log.Message("Log2");
                if (dinfo.Amount == 0)
                {
                    __result = DamageWorker.DamageResult.MakeNew();
                }
                //Log.Message("Log3");
                if (__instance.def.damageMultipliers != null)
                {
                    for (int i = 0; i < __instance.def.damageMultipliers.Count; i++)
                    {
                        if (__instance.def.damageMultipliers[i].damageDef == dinfo.Def)
                        {
                            int amount = Mathf.RoundToInt((float)dinfo.Amount * __instance.def.damageMultipliers[i].multiplier);
                            dinfo.SetAmount(amount);
                        }
                    }
                }
                //Log.Message("Log4");
                bool flag;
                __instance.PreApplyDamage(dinfo, out flag);
                if (flag)
                {
                    __result = DamageWorker.DamageResult.MakeNew();
                }
                //Log.Message("Log5");
                bool spawnedOrAnyParentSpawned = __instance.SpawnedOrAnyParentSpawned;
                Map mapHeld = __instance.MapHeld;
                DamageWorker.DamageResult result = dinfo.Def.Worker.Apply(dinfo, __instance);
                if (dinfo.Def.harmsHealth && spawnedOrAnyParentSpawned)
                {
                    mapHeld.damageWatcher.Notify_DamageTaken(__instance, result.totalDamageDealt);
                }
                //Log.Message("Log6");
                if (dinfo.Def.externalViolence)
                {
                    GenLeaving.DropFilthDueToDamage(__instance, result.totalDamageDealt);
                    if (dinfo.Instigator != null)
                    {
                        Pawn pawn = dinfo.Instigator as Pawn;
                        if (pawn != null)
                        {
                            pawn.records.AddTo(RecordDefOf.DamageDealt, result.totalDamageDealt);
                            pawn.records.AccumulateStoryEvent(StoryEventDefOf.DamageDealt);
                        }
                    }
                }
                //Log.Message("Log7");
                __instance.PostApplyDamage(dinfo, result.totalDamageDealt);
                __result = result;
                return false;
                //return true;
            }
        }


        //[HarmonyPostfix]
        //public static void TakeDamage_Postfix(Thing __instance, DamageInfo dinfo, DamageWorker.DamageResult __result)
        //{
        //    if (__instance as Pawn == null)
        //    {

        //        return;

        //    }
        //    String s = __instance.ToString();
        //    s += dinfo.AllowDamagePropagation + " ";
        //    s += dinfo.Amount + " ";
        //    s += dinfo.Angle + " ";
        //    s += dinfo.Category + " ";
        //    s += dinfo.Def.Worker + " ";
        //    s += dinfo.Depth + " ";
        //    s += dinfo.Height + " ";
        //    s += dinfo.HitPart + " ";
        //    s += dinfo.InstantOldInjury + " ";
        //    s += dinfo.Instigator + " ";
        //    s += dinfo.Weapon + " ";
        //    s += dinfo.WeaponBodyPartGroup + " ";
        //    s += dinfo.WeaponLinkedHediff + " ";
        //    Log.Message("TakeDamage "+s);
        //    String s2="";
        //    s2 += __result.deflected;
        //    s2 += __result.headshot;
        //    s2 += __result.LastHitPart;
        //    s2 += __result.parts;
        //    s2 += __result.totalDamageDealt;
        //    s2 += __result.wounded;
        //    s2 += __result.ToString();
        //    Log.Message("damageResult: " + s2);
        //    //return true;
        //}
    }

    //[HarmonyPatch(typeof(DamageWorker),"Apply",new Type[] { typeof(DamageInfo),typeof(Thing)})]
    //static class Patch_DamageWorker
    //{
    //    [HarmonyPrefix]
    //    public static void Apply_Prefix(DamageWorker __instance, DamageInfo dinfo, Thing victim)
    //    {
    //        Log.Message("Apply "+__instance.ToString() + victim);
    //    }
    //}

    //[HarmonyPatch(typeof(DamageWorker_AddInjury), "Apply", new Type[] { typeof(DamageInfo), typeof(Thing) })]
    //static class Patch_DamageWorker_AddInjury_Apply
    //{
    //    [HarmonyPrefix]
    //    public static void Apply_AddInjury_Prefix(DamageWorker __instance, DamageInfo dinfo, Thing thing)
    //    {
    //        Log.Message("Apply_AddInjury" + __instance.ToString() + thing);
    //    }
    //}

    //[HarmonyPatch(typeof(DamageWorker_AddInjury), "ApplyToPawn", new Type[] { typeof(DamageInfo), typeof(Pawn) })]
    //static class Patch_DamageWorker_AddInjury
    //{
    //    [HarmonyPrefix]
    //    public static void ApplyToPawn_Prefix(DamageWorker __instance, DamageInfo dinfo, Pawn pawn)
    //    {
    //        Log.Message("ApplyToPawn"+__instance.ToString() + pawn);
    //    }
    //}

    //[HarmonyPatch(typeof(Bill_Medical))]
    //[HarmonyPatch("CompletableEver", PropertyMethod.Getter)]
    //static class Patch_Bill_Medical_Compl
    //{
    //    [HarmonyPostfix]
    //    public static void postf(ref bool __result)
    //    {
    //        Log.Message("CompletableEver: " + __result);
    //    }
    //}

    

    //[HarmonyPatch(typeof(BillStack), "AddBill", new Type[] { typeof(Bill)})]
    //static class Patch_BillStack_AddBill
    //{
        
    //    [HarmonyPostfix]
    //    static void AddBill_Postfix(Bill bill)
    //    {
    //        Log.Message("AddBill: " + bill.ToString()+" of stack "+bill.billStack);
    //        //return true;
    //    }


    //}

    
}