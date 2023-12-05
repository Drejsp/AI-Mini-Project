using ChessChallenge.API;
using System;

public class MyBot : IChessBot
{
    private const int MaxDepth = 4;
    private int moveCounter = 0;

    // Piece-Square Tables for pawns and knights
    private readonly int[,] pawnTable = {
        { 0, 0, 0, 0, 0, 0, 0, 0 },
        { 5, 10, 10, -10, -10, 10, 10, 5 },
        { 5, -5, -10, 0, 0, -10, -5, 5 },
        { 0, 0, 0, 20, 20, 0, 0, 0 },
        { 5, 5, 10, 25, 25, 10, 5, 5 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 0, 0, 0, 0, 0, 0, 0, 0 }
    };
    private readonly int[,] knightTable = {
        {-50, -40, -30, -30, -30, -30, -40, -50},
        {-40, -20, 0, 0, 0, 0, -20, -40},
        {-30, 0, 10, 15, 15, 10, 0, -30},
        {-30, 5, 15, 20, 20, 15, 5, -30},
        {-30, 0, 15, 20, 20, 15, 0, -30},
        {-30, 5, 10, 15, 15, 10, 5, -30},
        {-40, -20, 0, 5, 5, 0, -20, -40},
        {-50, -40, -30, -30, -30, -30, -40, -50}
        };

    public Move Think(Board board, Timer timer)
    {
        moveCounter++;
        var result = AlphaBeta(board, MaxDepth, int.MinValue, int.MaxValue, true);
        return result.Item2 ?? new Move();
    }

    private Tuple<int, Move?> AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsInStalemate())
        {
            return Tuple.Create(EvaluateBoard(board), (Move?)null);
        }

        Move? bestMove = null;
        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, false).Item1;
                board.UndoMove(move);

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return Tuple.Create(maxEval, bestMove);
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, true).Item1;
                board.UndoMove(move);
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                }

                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return Tuple.Create(minEval, bestMove);
        }
    }

    private int EvaluateBoard(Board board)
    {
        int score = 0;

        // Encourage central control and piece development
        int centralControlBonus = 10;

        foreach (var piece in board.GetAllPieceLists())
        {
            foreach (var p in piece)
            {
                int value = GetPieceValue(p);

                // Add positional evaluation for pawns
                if (p.PieceType == PieceType.Pawn)
                {
                    value += pawnTable[p.Square.Rank, p.Square.File];

                    // Encourage central control for pawns
                    if (p.Square.File >= 3 && p.Square.File <= 4)
                        value += centralControlBonus;

                    // Discourage isolated pawns (simplified check)
                    if (!IsPawnIsolated(p, board))
                        value -= 20;
                }

                // Add positional evaluation for knights
                if (p.PieceType == PieceType.Knight)
                {
                    value += EvaluateKnightPosition(p.Square);
                }

                // Encourage piece development and penalize repeated moves
                if (board.PlyCount < 10 && p.PieceType != PieceType.Pawn)
                    value += centralControlBonus;

                // Discourage moves that seem risky (simplified check)
                if (IsMoveRisky(p, board))
                    value -= 50;

                // Adjust the value for pieces of the opposite color
                if (!p.IsWhite)
                    value *= -1;

                score += value;
            }
        }

        return score;
    }

    private bool IsPawnIsolated(Piece pawn, Board board)
    {
        int file = pawn.Square.File;
        int rank = pawn.Square.Rank;

        // Directly check adjacent squares for isolation
        bool leftIsolated = file > 0 && board.GetPiece(new Square(file - 1, rank)).PieceType == PieceType.None;
        bool rightIsolated = file < 7 && board.GetPiece(new Square(file + 1, rank)).PieceType == PieceType.None;

        return leftIsolated && rightIsolated;
    }

    private bool IsMoveRisky(Piece piece, Board board)
    {
        return piece.PieceType != PieceType.King && RandomDecision();
    }

    private bool RandomDecision()
    {
        return new Random().Next(2) == 0;
    }

    private int EvaluateKnightPosition(Square square)
    {
        // Ensure the square is within bounds
        int rank = Math.Max(0, Math.Min(7, square.Rank));
        int file = Math.Max(0, Math.Min(7, square.File));

        return knightTable[rank, file];
    }

    private int GetPieceValue(Piece piece)
    {
        switch (piece.PieceType)
        {
            case PieceType.Pawn: return 100;
            case PieceType.Knight: return 320;
            case PieceType.Bishop: return 330;
            case PieceType.Rook: return 500;
            case PieceType.Queen: return 900; // Increase the weight for the queen
            case PieceType.King: return 20000; // Assign a high weight to the king to prioritize its safety
            default: return 0;
        }
    }
}
