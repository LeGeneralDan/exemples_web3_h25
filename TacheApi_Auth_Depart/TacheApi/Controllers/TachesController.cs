﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TacheApi.Models;
using TacheApi.Services;

namespace TacheApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TachesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TachesController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize]
        [EndpointSummary("Récupère toutes les tâches de l'utilisateur")]
        [EndpointDescription("Récupère toutes les tâches de l'utilisateur de la base de données")]
        [ProducesResponseType<IEnumerable<TacheDTO>>(StatusCodes.Status200OK, "application/json")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TacheDTO>>> GetTaches()
        {
            return await _context.Taches // Récupère les tâches
                .Select(tache => new TacheDTO(tache)) // Transforme les tâches en TacheDTO
                .ToListAsync(); // Récupère les tâches sous forme de liste
        }

        [Authorize]
        [EndpointSummary("Récupère une tâche de l'utilisateur")]
        [EndpointDescription("Récupère une tâche de l'utilisateur de la base de données en fonction de son identifiant")]
        [ProducesResponseType<TacheDetailsDTO>(StatusCodes.Status200OK, "application/json")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpGet("{id}")]
        public async Task<ActionResult<TacheDetailsDTO>> GetTache(
            [Description("L'identifiant de la tâche à récupérer")] long id)
        {
            var tache = await _context.Taches
                .Include(tache => tache.Etapes)
                .SingleOrDefaultAsync(tache => tache.Id == id);

            if (tache == null)
            {
                return NotFound();
            }

            return new TacheDetailsDTO(tache);
        }

        [Authorize]
        [EndpointSummary("Met à jour une tâche de l'utilisateur")]
        [EndpointDescription("Met à jour une tâche de l'utilisateur de la base de données en fonction de son identifiant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTache(
            [Description("L'identifiant de la tâche à modifier")] long id,
            [FromBody][Description("La tâche modifiée")] TacheUpsertDTO tacheDTO)
        {
            var tache = await _context.Taches.FindAsync(id);

            if (tache == null)
            {
                return NotFound();
            }

            tache.AppliquerUpsertDTO(tacheDTO);
            _context.Entry(tache).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [EndpointSummary("Ajoute une tâche à l'utilisateur")]
        [EndpointDescription("Ajoute une tâche à l'utilisateur dans la base de données")]
        [ProducesResponseType<TacheDTO>(StatusCodes.Status201Created, "application/json")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [HttpPost]
        public async Task<ActionResult<TacheDTO>> PostTache(
            [FromBody][Description("La tâche à ajouter")] TacheUpsertDTO tacheDTO)
        {
            Tache tache = new Tache(
                tacheDTO
            );
            _context.Taches.Add(tache);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetTache),
                new { id = tache.Id },
                new TacheDTO(tache)
            );
        }

        [Authorize("delete:taches")]
        [EndpointSummary("Supprime une tâche")]
        [EndpointDescription("Supprime une tâche de la base de données en fonction de son identifiant")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTache(
              [Description("L'identifiant de la tâche à supprimer")] long id)
        {
            var tache = await _context.Taches
                .Include(tache => tache.Etapes)
                .SingleOrDefaultAsync(tache => tache.Id == id);

            if (tache == null)
            {
                return NotFound();
            }

            _context.Taches.Remove(tache);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
