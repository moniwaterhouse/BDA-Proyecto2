using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TiendaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : Controller
    {
        private readonly IDriver _driver;

        public ClientesController()
        {
            _driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "1234"));
        }

        [HttpPost("loadCSV")]
        public async Task<IActionResult> LoadClientsCSV(string csvFilePath)
        {
            var statementText = new StringBuilder();
            statementText.Append("LOAD CSV WITH HEADERS FROM 'file:///" + csvFilePath + "' AS row\nWITH row WHERE row.id IS NOT NULL\nMERGE (c:Clientes {id: toInteger(row.id), first_name: row.first_name, last_name: row.last_name})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "El grafo de clientes fue creado correctamente");
        }

        [HttpPost("initClientRelations")]
        public async Task<IActionResult> InitClientRelations()
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (c:Clientes)\nUNWIND c.id as clienteIds\nMATCH (p:Compras {idCliente:clienteIds})\nCREATE (c)-[r:realizo]->(p)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se ha creado correctamente la relacion cliente-realizo->compra");
        }

        [HttpPost("crearCliente")]
        public async Task<IActionResult> CrearCliente(int id, string firstName, string lastName)
        {
            var statementText = new StringBuilder();
            statementText.Append("CREATE (c:Clientes {id :" + id + ", first_name : '" +firstName +"', last_name : '" +lastName+"'})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se ha agregado correctamente el nuevo cliente");
        }

        [HttpDelete("eliminarCliente")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            var statementText = new StringBuilder();
            statementText.Append("match(c:Clientes {id:"+id+"})\noptional MATCH (c)-[r:realizo]-()\noptional MATCH (p:Compras {idCliente:"+id+"})\noptional MATCH (p)-[r2:contiene]-()\ndelete c, r, p, r2");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se ha borrado correctamente el cliente");
        }

        [HttpPut("modificarCliente")]
        public async Task<IActionResult> ModificarCliente(int id, string firstName, string lastName)
        {
            var statementText = new StringBuilder();
            statementText.Append("match (c:Clientes {id : "+id+"})\nset c = {id:"+id+", first_name : '"+firstName+"', last_name:'"+lastName+"'}");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201, "Se ha modificado el cliente correctamente");
        }


    }


}

