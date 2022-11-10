using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neo4j.Driver;
using System.Text;
using HR.Models;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.IO;
using System.Numerics;
using Newtonsoft.Json;
using TiendaAPI.Models;

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
            return StatusCode(201);
        }

        [HttpPost("initClientRelations")]
        public async Task<IActionResult> InitClientRelations()
        {
            var statementText = new StringBuilder();
            statementText.Append("MATCH (c:Clientes)\nUNWIND c.id as clienteIds\nMATCH (p:Compras {idCliente:clienteIds})\nCREATE (c)-[r:realizo]->(p)");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPost("crearCliente")]
        public async Task<IActionResult> CrearCliente(int id, string firstName, string lastName)
        {
            var statementText = new StringBuilder();
            statementText.Append("CREATE (c:Clientes {id :" + id + ", first_name : '" +firstName +"', last_name : '" +lastName+"'})");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpDelete("eliminarCliente")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            var statementText = new StringBuilder();
            statementText.Append("match(c:Clientes {id:"+id+"})\noptional MATCH (c)-[r:realizo]-()\noptional MATCH (p:Compras {idCliente:"+id+"})\noptional MATCH (p)-[r2:contiene]-()\ndelete c, r, p, r2");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpPut("modificarCliente")]
        public async Task<IActionResult> ModificarCliente(int id, string firstName, string lastName)
        {
            var statementText = new StringBuilder();
            statementText.Append("match (c:Clientes {id : "+id+"})\nset c = {id:"+id+", first_name : '"+firstName+"', last_name:'"+lastName+"'}");
            var session = this._driver.AsyncSession();
            var result = await session.WriteTransactionAsync(tx => tx.RunAsync(statementText.ToString()));
            return StatusCode(201);
        }

        [HttpGet("getAllClients")]
        public async Task<IActionResult> GetAllClients()
        {


            IResultCursor cursor;
            var resultados = new List<INode>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var clientes = new List<Cliente>();
            try
            {
                cursor = await session.RunAsync(@"MATCH (c:Clientes) RETURN c");
                resultados= await cursor.ToListAsync(record =>
                    record[0].As<INode>());

            }
            finally
            {
                await session.CloseAsync();
            }

            foreach (var result in resultados)
            {
                var nodeProps = JsonConvert.SerializeObject(result.As<INode>().Properties);
                clientes.Add(JsonConvert.DeserializeObject<Cliente>(nodeProps));
            }
            return Ok(clientes);
        }

        [HttpGet("getTopClientes")]
        public async Task<IActionResult> GetTopClientes()
        {


            IResultCursor cursor;
            var resultados = new List<IRecord>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var marcas = new List<TopCliente>();
            try
            {
                cursor = await session.RunAsync(@"match (cliente:Clientes)-[r:realizo]-(compra:Compras)
                                                  return cliente.first_name as nombre, cliente.last_name as apellido, sum(compra.cantidad) as cantidadProductos
                                                  order by cantidadProductos desc limit 5");
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
                marcas.Add(JsonConvert.DeserializeObject<TopCliente>(props));

            }
            return Ok(marcas);
        }

        [HttpGet("getClientProducts")]
        public async Task<IActionResult> GetClientProducts(int id)
        {


            IResultCursor cursor;
            var resultados = new List<IRecord>();
            var statementText = new StringBuilder();
            var session = this._driver.AsyncSession();
            var marcas = new List<TopProducto>();
            try
            {
                cursor = await session.RunAsync(@"match (cliente:Clientes{id:"+id+"})-[r:realizo]-(compra:Compras)-[r2:contiene]-(producto:Productos) return producto.nombre as nombre, compra.cantidad as unidades");
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
                marcas.Add(JsonConvert.DeserializeObject<TopProducto>(props));

            }
            return Ok(marcas);
        }




    }


    }

