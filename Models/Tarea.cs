namespace task_manager.Models
{
    public class Tarea
    {
        public int Id { get; set; } // Identificador único de la tarea
        public string Titulo { get; set; } = string.Empty; // Título de la tarea
        public string Descripcion { get; set; } = string.Empty; // Descripción de la tarea
        public bool EstaCompletada { get; set; } // Estado de la tarea (completada o no)
        public int UsuarioId { get; set; } // Relación con el Usuario
    }
}

