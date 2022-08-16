using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace Armiger
{
    public class ArmigerModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ArmigerComponent>();
        }
    }
    public class ArmigerComponent : MonoBehaviour
    {
        Item item;
        bool warp = false;
        GameObject tkhandle;

        public void Start()
        {
            item = GetComponent<Item>();
            tkhandle = item.GetCustomReference("TKHandle").gameObject;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;
            tkhandle.SetActive(false);
        }

        private void Item_OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            teleGrabber.spinFromCenterOfMass = true;
            if (warp)
            {
                StartCoroutine(Warp(teleGrabber.spellCaster.ragdollHand, handle));
            }
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            warp = false;
            tkhandle.SetActive(false);
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            if (PlayerControl.GetHand(ragdollHand.side).castPressed || PlayerControl.GetHand(ragdollHand.side).alternateUsePressed)
            {
                warp = true;
                tkhandle.SetActive(true);
                foreach (Damager damager in item.GetComponentsInChildren<Damager>())
                {
                    damager.data.penetrationTempModifierDamperOut += 100000;
                }
                item.IgnoreRagdollCollision(ragdollHand.ragdoll);
            }
            else
            {
                warp = false;
                tkhandle.SetActive(false);
            }
        }
        public void FixedUpdate()
        {
            if (item.data.id == "StarOfTheRogue" && item.isFlying)
            {
                //Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 2160, 0) * Time.fixedDeltaTime);
                //item.rb.MoveRotation(item.rb.rotation * deltaRotation);
                item.flyDirRef.Rotate(new Vector3(0, -2160, 0) * Time.fixedDeltaTime);
            }
        }
        public IEnumerator Warp(RagdollHand hand, Handle handle)
        {
            yield return new WaitForEndOfFrame();
            hand.caster.telekinesis.TryRelease();
            warp = false;
            Quaternion rotation = Player.local.transform.rotation;
            Common.MoveAlign(Player.local.transform, hand.grip, item.GetMainHandle(hand.side).GetDefaultOrientation(hand.side).transform);
            Player.local.transform.rotation = rotation;
            //Player.local.transform.position = handle.transform.position;
            Player.local.locomotion.prevPosition = handle.transform.position;
            Player.local.locomotion.rb.velocity = Vector3.zero;
            Player.local.locomotion.velocity = Vector3.zero;
            if (!item.isPenetrating) Common.MoveAlign(item.transform, item.GetMainHandle(hand.side).GetDefaultOrientation(hand.side).transform, hand.grip);
            if (hand.grabbedHandle == null)
                hand.Grab(item.GetMainHandle(hand.side));
            yield return new WaitForSeconds(0.5f);
            Player.local.locomotion.rb.velocity = Vector3.zero;
            Player.local.locomotion.velocity = Vector3.zero;
            foreach (Damager damager in item.GetComponentsInChildren<Damager>())
            {
                damager.data.penetrationTempModifierDamperOut -= 100000;
            }
            yield break;
        }
    }
}
