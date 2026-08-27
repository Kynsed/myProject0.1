// NOTE: shim de audio. FMOD nao portado (conteudo). Tipo minimo (classe p/ permitir null-checks).
namespace FMOD.Studio
{
    public class EventInstance
    {
        public void release() { }
        public void setParameterValue(string name, float value) { }
    }
}
