using UnityEngine;

namespace DragonMerge.Logic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int startMoves = 30;

        public int MovesLeft { get; private set; }
        public int Score { get; private set; }
        public bool IsGameOver => MovesLeft <= 0;

        private void Awake()
        {
            MovesLeft = startMoves;
        }

        public void SpendMove()
        {
            MovesLeft = Mathf.Max(0, MovesLeft - 1);
            Debug.Log($"Moves: {MovesLeft}");
            if (MovesLeft <= 0)
            {
                Debug.Log($"Fim de jogo! Pontuação final: {Score}");
            }
        }

        public void AddScore(int amount)
        {
            Score += amount;
            Debug.Log($"Score: {Score}");
        }
    }
}
