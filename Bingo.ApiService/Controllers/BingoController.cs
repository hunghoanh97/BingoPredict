using Microsoft.AspNetCore.Mvc;
using Bingo.ApiService.Services;
using Bingo.ApiService.Models;
using Bingo.ApiService.Models.Entities;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bingo.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BingoController : ControllerBase
    {
        private readonly IBingoService _bingoService;
        private readonly IGameService _gameService;
        private readonly IBettingService _bettingService;
        private readonly IStatisticsService _statisticsService;

        public BingoController(
            IBingoService bingoService,
            IGameService gameService,
            IBettingService bettingService,
            IStatisticsService statisticsService)
        {
            _bingoService = bingoService;
            _gameService = gameService;
            _bettingService = bettingService;
            _statisticsService = statisticsService;
        }

        [HttpGet("predict")]
        public async Task<ActionResult<PredictionResult>> PredictNextSum()
        {
            try
            {
                var result = await _bingoService.PredictNextSumAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("check-prediction")]
        public async Task<ActionResult<PredictionAccuracyResult>> CheckPredictionAccuracy()
        {
            try
            {
                var result = await _bingoService.CheckPredictionAccuracyAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("games/current")]
        public async Task<ActionResult<Game>> GetCurrentGame()
        {
            try
            {
                var game = await _gameService.GetCurrentGameAsync();
                return game != null ? Ok(game) : NotFound("No current game found");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("games/recent")]
        public async Task<ActionResult<IEnumerable<Game>>> GetRecentGames([FromQuery] int limit = 10)
        {
            try
            {
                var games = await _gameService.GetRecentGamesAsync(limit);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("games/{gameId}/draw")]
        public async Task<ActionResult<GameResult>> DrawNumbers(Guid gameId)
        {
            try
            {
                var result = await _gameService.DrawNumbersAsync(gameId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("bets")]
        public async Task<ActionResult<Bet>> PlaceBet([FromBody] PlaceBetRequest request)
        {
            try
            {
                var bet = await _bettingService.PlaceBetAsync(request);
                return Ok(bet);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("players/{playerId}/bets")]
        public async Task<ActionResult<IEnumerable<Bet>>> GetPlayerBets(Guid playerId, [FromQuery] int limit = 50)
        {
            try
            {
                var bets = await _bettingService.GetPlayerBetsAsync(playerId, limit);
                return Ok(bets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("statistics/numbers")]
        public async Task<ActionResult<Dictionary<int, int>>> GetNumberFrequency([FromQuery] int days = 30)
        {
            try
            {
                var frequency = await _statisticsService.GetNumberFrequencyAsync(days);
                return Ok(frequency);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("players/{playerId}/statistics")]
        public async Task<ActionResult<PlayerStatsDto>> GetPlayerStatistics(Guid playerId)
        {
            try
            {
                var stats = await _statisticsService.GetPlayerStatisticsAsync(playerId);
                return Ok(stats);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("statistics/games")]
        public async Task<ActionResult<GameStatsDto>> GetGameStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetGameStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("statistics/predictions")]
        public async Task<ActionResult<PredictionStatsDto>> GetPredictionStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetPredictionStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}