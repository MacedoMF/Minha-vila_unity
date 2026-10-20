using System.Collections;
using UnityEngine;

namespace MinhaVila.Unity
{
    public sealed class MusicPlayer : MonoBehaviour
    {
        private AudioSource _firstSource;
        private AudioSource _secondSource;
        private AudioSource _activeSource;
        private AudioClip _menuTrack;
        private AudioClip _villageTrack;
        private bool _enabled;
        private float _volume = 0.65f;
        private Coroutine _transition;

        public bool IsEnabled { get { return _enabled; } }
        public float Volume { get { return _volume; } }
        public bool HasTracks { get { return _menuTrack != null || _villageTrack != null; } }

        public void Initialize()
        {
            _menuTrack = Resources.Load<AudioClip>("Audio/music-menu");
            _villageTrack = Resources.Load<AudioClip>("Audio/music-village");
            _firstSource = gameObject.AddComponent<AudioSource>();
            _secondSource = gameObject.AddComponent<AudioSource>();
            Configure(_firstSource);
            Configure(_secondSource);
            _activeSource = _firstSource;
            _enabled = PlayerPrefs.GetInt("minhavila.music.enabled", 0) == 1;
            _volume = PlayerPrefs.GetFloat("minhavila.music.volume", 0.65f);
            SetVolume(_volume);
        }

        public bool Toggle()
        {
            if (!_enabled && !HasTracks) return false;
            _enabled = !_enabled;
            PlayerPrefs.SetInt("minhavila.music.enabled", _enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (_enabled)
            {
                PlayTrack(_menuTrack, false);
            }
            else
            {
                StopAll();
            }
            return _enabled;
        }

        public void SetInVillage(bool inVillage)
        {
            if (!_enabled) return;
            PlayTrack(inVillage ? _villageTrack : _menuTrack, true);
        }

        public void SetVolume(float volume)
        {
            _volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("minhavila.music.volume", _volume);
            PlayerPrefs.Save();
            if (_firstSource != null) _firstSource.volume = _volume;
            if (_secondSource != null) _secondSource.volume = _volume;
        }

        private void Configure(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0;
            source.spatialBlend = 0;
        }

        private void PlayTrack(AudioClip clip, bool crossfade)
        {
            if (clip == null) return;
            if (_activeSource.isPlaying && _activeSource.clip == clip) return;
            if (_transition != null) StopCoroutine(_transition);

            AudioSource next = _activeSource == _firstSource ? _secondSource : _firstSource;
            next.clip = clip;
            next.volume = crossfade ? 0 : _volume;
            next.Play();
            if (!crossfade)
            {
                _activeSource.Stop();
                _activeSource.volume = 0;
                _activeSource = next;
                return;
            }

            _transition = StartCoroutine(Crossfade(_activeSource, next));
        }

        private IEnumerator Crossfade(AudioSource previous, AudioSource next)
        {
            const float duration = 1.6f;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float blend = Mathf.Clamp01(elapsed / duration);
                previous.volume = _volume * (1 - blend);
                next.volume = _volume * blend;
                yield return null;
            }

            previous.Stop();
            previous.volume = 0;
            next.volume = _volume;
            _activeSource = next;
            _transition = null;
        }

        private void StopAll()
        {
            if (_transition != null)
            {
                StopCoroutine(_transition);
                _transition = null;
            }

            _firstSource.Stop();
            _secondSource.Stop();
            _firstSource.volume = 0;
            _secondSource.volume = 0;
        }
    }
}
