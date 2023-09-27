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
        Handle handle;

        public void Start()
        {
            item = GetComponent<Item>();
            tkhandle = item.GetCustomReference("TKHandle").gameObject;
            handle = tkhandle.GetComponent<Handle>();
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;
            item.disallowDespawn = true;
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
        public void Update()
        {
            if (item.data?.id == "StarOfTheRogue" && item.isFlying)
            {
                item.flyDirRef.Rotate(new Vector3(0, -2160, 0) * Time.deltaTime);
            }
            if (Vector3.Distance(tkhandle.transform.position, Player.local.head.transform.position) < Player.local.creature.mana.casterRight?.telekinesis?.maxCatchDistance + (handle.touchCollider as SphereCollider).radius)
                (handle.touchCollider as SphereCollider).radius = Mathf.Max(Vector3.Distance(tkhandle.transform.position, Player.local.head.transform.position) * 0.5f, 1f);
            else (handle.touchCollider as SphereCollider).radius = Vector3.Distance(tkhandle.transform.position, Player.local.head.transform.position) - Player.local.creature.mana.casterRight.telekinesis.maxCatchDistance + 0.1f;
        }
        public IEnumerator Warp(RagdollHand hand, Handle handle)
        {
            yield return null;
            hand.caster.telekinesis.TryRelease();
            warp = false;
            item.physicBody.velocity = Vector3.zero;
            Quaternion rotation = Player.local.transform.rotation;
            Vector3 position = Player.local.transform.position;
            Common.MoveAlign(Player.local.transform, hand.grip, item.GetMainHandle(hand.side).GetDefaultOrientation(hand.side).transform);
            Player.local.transform.rotation = rotation;
            Player.local.locomotion.prevPosition = handle.transform.position;
            if (!item.isPenetrating) Common.MoveAlign(item.transform, item.GetMainHandle(hand.side).GetDefaultOrientation(hand.side).transform, hand.grip);
            if (hand.grabbedHandle == null)
                hand.Grab(item.GetMainHandle(hand.side));
            Player.local.creature.ragdoll.ik.AddLocomotionDeltaPosition(Player.local.transform.position - position);
            Player.local.creature.ragdoll.ik.AddLocomotionDeltaRotation(Player.local.transform.rotation * Quaternion.Inverse(rotation), Player.local.transform.TransformPoint(Player.local.creature.transform.localPosition));
            Player.local.locomotion.rb.velocity = Vector3.zero;
            Player.local.locomotion.velocity = Vector3.zero;
            foreach (RagdollPart part in Player.local.creature.ragdoll.parts) part.physicBody.velocity = Vector3.zero;
            yield return new WaitForSecondsRealtime(0.1f);
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
