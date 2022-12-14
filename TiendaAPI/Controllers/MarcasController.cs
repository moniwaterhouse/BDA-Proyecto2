using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;
using Newtonsoft.Json;
using TiendaAPI.Models;
using HR.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TiendaAPI.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    public class MarcasController : Controller
    {

        private readonly IDriver _driver;

        public MarcasController()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1234"));
        }

        [HttpPost("loadCSV")]
        public async Task<IActionResult> LoadBrandsCSV(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.id IS NOT NULL\nMERGE (m:Marcas {id: toInteger(row.id), nombre : row.nombre, pais : row.pais})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpGet("getAllMarcas")]
        public async Task<IActionResult> GetAllMarcas()
        {


            IResultCursor cursor;
            var resultados = new List<INode>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var marcas = new List<Marca>();
            try
            {
                cursor = await session.RunAsync(@"MATCH (m:Marcas) RETURN m");
                resultados = await cursor.ToListAsync(record =>
                    record[0].As<INode>());

            }
            finally
            {
                await session.CloseAsync();
            }

            foreach (var result in resultados)
            {
                var nodeProps = JsonConvert.SerializeObject(result.As<INode>().Properties);
                marcas.Add(JsonConvert.DeserializeObject<Marca>(nodeProps));
            }
            return Ok(marcas);
        }

        [HttpGet("getMarcasMasVendidas")]
        public async Task<IActionResult> GetMarcasMasVendidas()
        {


            IResultCursor cursor;
            var resultados = new List<IRecord>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var marcas = new List<TopMarca>();
            try
            {
                cursor = await session.RunAsync(@"match (compra:Compras)-[r:contiene]-(producto:Productos)-[r2:elaborado_por]-(marca:Marcas)
                                                  return marca.nombre as nombre, marca.pais as pais, sum(compra.cantidad) as unidadesVendidas
                                                  order by unidadesVendidas desc limit 5");
                resultados = await cursor.ToListAsync(record =>
                    record.As<IRecord>());

            }
            finally
            {
                await session.CloseAsync();
            }

            foreach (var result in resultados)
            {
                var props = JsonConvert.SerializeObject(result.As<IRecord>().Values);
                marcas.Add(JsonConvert.DeserializeObject<TopMarca>(props));

            }
            return Ok(marcas);
        }

    }

   


}

