using Microsoft.AspNetCore.Mvc;
using Bingo.ApiService.Services;
using Bingo.ApiService.Models;
using System;
using System.Threading.Tasks;

namespace Bingo.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BingoController : ControllerBase
    {
        private readonly IBingoService _bingoService;

        public BingoController(IBingoService bingoService)
        {
            _bingoService = bingoService;
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
    }
}