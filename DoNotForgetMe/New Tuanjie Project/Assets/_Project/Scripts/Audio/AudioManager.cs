using UnityEngine;

namespace DoNotForgetMe.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioLibrary library;

        private AudioSource _sfxSource;
        private AudioSource _bgmSource;
        private AudioSource _ambSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.loop = false;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = 0.5f;

            _ambSource = gameObject.AddComponent<AudioSource>();
            _ambSource.playOnAwake = false;
            _ambSource.loop = true;
            _ambSource.volume = 0.3f;
        }

        public static void Play(SfxId id)
        {
            if (Instance == null || Instance.library == null) return;
            var clip = Instance.library.GetSfx(id);
            if (clip != null)
                Instance._sfxSource.PlayOneShot(clip);
        }

        public static void PlayBgm(BgmId id)
        {
            if (Instance == null || Instance.library == null) return;
            var clip = Instance.library.GetBgm(id);
            if (clip == null) return;
            if (Instance._bgmSource.clip == clip) return;
            Instance._bgmSource.clip = clip;
            Instance._bgmSource.Play();
        }

        public static void StopBgm()
        {
            if (Instance == null) return;
            Instance._bgmSource.Stop();
        }

        public static void PlayAmb(AmbId id)
        {
            if (Instance == null || Instance.library == null) return;
            var clip = Instance.library.GetAmb(id);
            if (clip == null) return;
            if (Instance._ambSource.clip == clip) return;
            Instance._ambSource.clip = clip;
            Instance._ambSource.Play();
        }

        public static void StopAmb()
        {
            if (Instance == null) return;
            Instance._ambSource.Stop();
        }
    }
}
