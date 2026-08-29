using System;
using UnityEngine;

namespace DoNotForgetMe.Audio
{
    [CreateAssetMenu(menuName = "Data/Audio Library", fileName = "AudioLibrary")]
    public class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct SfxEntry
        {
            public SfxId id;
            public AudioClip clip;
        }

        [Serializable]
        public struct BgmEntry
        {
            public BgmId id;
            public AudioClip clip;
        }

        [Serializable]
        public struct AmbEntry
        {
            public AmbId id;
            public AudioClip clip;
        }

        [SerializeField] private SfxEntry[] sfxEntries;
        [SerializeField] private BgmEntry[] bgmEntries;
        [SerializeField] private AmbEntry[] ambEntries;

        public AudioClip GetSfx(SfxId id)
        {
            if (sfxEntries == null) return null;
            foreach (var e in sfxEntries)
                if (e.id == id) return e.clip;
            return null;
        }

        public AudioClip GetBgm(BgmId id)
        {
            if (bgmEntries == null) return null;
            foreach (var e in bgmEntries)
                if (e.id == id) return e.clip;
            return null;
        }

        public AudioClip GetAmb(AmbId id)
        {
            if (ambEntries == null) return null;
            foreach (var e in ambEntries)
                if (e.id == id) return e.clip;
            return null;
        }
    }
}
