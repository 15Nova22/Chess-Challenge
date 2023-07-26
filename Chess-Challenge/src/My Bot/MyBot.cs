
﻿using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int takenPieces = 0;
    int lostPieces = 0;
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        //Array mit möglichen Zügen zum schlagen, wenn schlagen möglich ist, wird geschlagen
        Move[] captureMoves = board.GetLegalMoves(true);

        //Alle Züge zum Schlagen Sortiert nach ihrem Wert Unterschied
        Move bestCapture = captureMoves
            //Nur positive Trades nehmen
            .Where(move => TradeValue(board, move) > 0)
            //Sortieren nach bestem Trade
            .OrderBy(move => TradeValue(board, move)
        //Zug mit bestem Wertverhältnis nehmen
        ).FirstOrDefault();
        //Valider Zug
        if (!bestCapture.IsNull) return bestCapture;
        
        //TODO Save attacked pieces

        //Zufälliger Zug wenn sonst nichts machbar ist
        Random rng = new();
        return allMoves[rng.Next(allMoves.Length)];
    }

    /// <summary>
    /// Calculates the Trade Value for a move
    /// </summary>
    /// <param name="board">The board</param>
    /// <param name="move">The move to play</param>
    /// <returns>
    ///     The value difference between the capturing piece and the captured piece or just the value of the captured piece if no recapture is possible
    /// </returns>
    int TradeValue(Board board, Move move)
    {
        return pieceValues[(int)move.CapturePieceType] - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
    }
}