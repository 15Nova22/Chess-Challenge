
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
    int counterRochade = 0;
    String lastMove;
    String[,] eroeffnung = new string[,] {  {"g1f3","b1c3","e2e4","d2d4","f1d3","c1e3","e1g1"} , {"g8f6", "b8c6","d7d5","e7e5","f8d6","c8e6","e8g8"}  };
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
        if (!bestCapture.IsNull) {
            if (bestCapture.MovePieceType != PieceType.Pawn) {
                lastMove = bestCapture.TargetSquare.Name + bestCapture.StartSquare.Name;
            }
            return bestCapture;
        }

        //Routine für Rochade, fester Ablauf, je nach Farbe
        if (counterRochade < eroeffnung.Length) {
            //Hat eine Figur während der Rochade geschlagen und wird vom Gegner bedroht, wird der Zug zurückgenommen
            if (lastMove is not null && board.SquareIsAttackedByOpponent(new Square(lastMove.Substring(0,2)))) {
                Move moveTemp = new Move(lastMove, board);
                return moveTemp;
            }
            int farbe = board.IsWhiteToMove ? 0 : 1;
            Move moveRochade = new Move(eroeffnung[farbe ,counterRochade], board);
            counterRochade++;
            return moveRochade;
        }


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