import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import Icon from '../components/Icon';
import PageShell from '../components/PageShell';
import StatusBadge from '../components/StatusBadge';
import { useAuth } from '../context/AuthContext';
import { useNotificacoes } from '../context/NotificacoesContext';
import { listarPasseios } from '../services/passeioService';
import { listarPets } from '../services/petService';
import { listarPasseadores } from '../services/passeadorService';
import { PAPEL, STATUS_PASSEIO, TIPO_PERFIL } from '../constants/enums';
import '../styles/home.css';

const FORMATO_DATA_HORA = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'short',
  day: '2-digit',
  month: 'short',
  hour: '2-digit',
  minute: '2-digit'
});

function saudacao() {
  const hora = new Date().getHours();

  if (hora < 12) {
    return 'Bom dia';
  }

  return hora < 18 ? 'Boa tarde' : 'Boa noite';
}

function Indicador({ icone, valor, rotulo, tom, para }) {
  const conteudo = (
    <>
      <span className={`indicador-icone indicador-icone--${tom}`}>
        <Icon nome={icone} />
      </span>
      <span className="indicador-valor">{valor ?? '–'}</span>
      <span className="indicador-rotulo">{rotulo}</span>
    </>
  );

  return para ? (
    <Link className="indicador" to={para}>
      {conteudo}
    </Link>
  ) : (
    <div className="indicador">{conteudo}</div>
  );
}

function AcaoRapida({ para, icone, titulo, texto }) {
  return (
    <Link className="acao-rapida" to={para}>
      <span className="acao-rapida-icone">
        <Icon nome={icone} />
      </span>
      <span className="acao-rapida-texto">
        <strong>{titulo}</strong>
        <small>{texto}</small>
      </span>
      <Icon nome="arrow" className="acao-rapida-seta" />
    </Link>
  );
}

const HomePage = () => {
  const { usuario } = useAuth();
  const { notificacoes, naoLidas } = useNotificacoes();

  const ehTutor =
    usuario.tipoPerfil === TIPO_PERFIL.TUTOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;
  const ehPasseador =
    usuario.tipoPerfil === TIPO_PERFIL.PASSEADOR || usuario.tipoPerfil === TIPO_PERFIL.AMBOS;

  const [passeios, setPasseios] = useState(null);
  const [pets, setPets] = useState(null);
  const [passeadores, setPasseadores] = useState(null);
  const [erro, setErro] = useState('');

  useEffect(() => {
    let ativo = true;

    // Cada bloco carrega por conta própria: uma falha não derruba o painel inteiro.
    listarPasseios(usuario.id)
      .then((lista) => ativo && setPasseios(lista))
      .catch((falha) => ativo && setErro(falha.message));

    if (ehTutor) {
      listarPets(usuario.id)
        .then((lista) => ativo && setPets(lista))
        .catch((falha) => ativo && setErro(falha.message));

      listarPasseadores()
        .then((lista) => ativo && setPasseadores(lista.filter((item) => item.id !== usuario.id)))
        .catch((falha) => ativo && setErro(falha.message));
    }

    return () => {
      ativo = false;
    };
  }, [usuario.id, ehTutor]);

  const contar = (papel, status) =>
    passeios ? passeios.filter((p) => p.papel === papel && p.status === status).length : null;

  const proximo = passeios
    ? [...passeios]
        .filter(
          (p) =>
            (p.status === STATUS_PASSEIO.ACEITO || p.status === STATUS_PASSEIO.SOLICITADO) &&
            new Date(p.dataHora) > new Date()
        )
        .sort((a, b) => new Date(a.dataHora) - new Date(b.dataHora))[0]
    : null;

  const recentes = passeios
    ? [...passeios].sort((a, b) => new Date(b.dataHora) - new Date(a.dataHora)).slice(0, 3)
    : [];

  const primeiroNome = usuario.nome.trim().split(' ')[0];

  return (
    <PageShell>
      <section className="boas-vindas">
        <div className="boas-vindas-texto">
          <span className="pill">
            <Icon nome="pata" />
            {ehTutor && ehPasseador ? 'Tutor e passeador' : ehTutor ? 'Tutor' : 'Passeador'}
          </span>
          <h1>
            {saudacao()}, {primeiroNome}!
          </h1>
          <p>
            {ehTutor
              ? 'Veja como estão os passeios dos seus pets.'
              : 'Veja as solicitações e os passeios que você tem pela frente.'}
          </p>
        </div>

        <div className="boas-vindas-proximo">
          <p className="proximo-titulo">
            <Icon nome="calendar" />
            Próximo passeio
          </p>

          {passeios === null && !erro && <div className="skeleton" style={{ height: 56 }} />}

          {passeios !== null && !proximo && (
            <p className="proximo-vazio">
              Nenhum passeio agendado por enquanto.
              {ehTutor && (
                <>
                  {' '}
                  <Link to="/passeadores">Encontrar passeador</Link>
                </>
              )}
            </p>
          )}

          {proximo && (
            <div className="proximo-dados">
              <strong>{proximo.nomePet}</strong>
              <span>{FORMATO_DATA_HORA.format(new Date(proximo.dataHora))}</span>
              <StatusBadge status={proximo.status} />
            </div>
          )}
        </div>
      </section>

      {erro && (
        <p className="alerta alerta--erro" role="alert">
          <Icon nome="alert" />
          Não foi possível carregar tudo agora: {erro}
        </p>
      )}

      {ehTutor && (
        <section className="painel-secao" aria-labelledby="titulo-tutor">
          <h2 id="titulo-tutor">{ehPasseador ? 'Como tutor' : 'Resumo'}</h2>

          <div className="indicadores">
            <Indicador
              icone="pata"
              tom="verde"
              valor={pets ? pets.length : null}
              rotulo={pets && pets.length === 1 ? 'Pet cadastrado' : 'Pets cadastrados'}
              para="/pets"
            />
            <Indicador
              icone="clock"
              tom="ambar"
              valor={contar(PAPEL.TUTOR, STATUS_PASSEIO.SOLICITADO)}
              rotulo="Aguardando resposta"
              para="/passeios"
            />
            <Indicador
              icone="play"
              tom="verde"
              valor={contar(PAPEL.TUTOR, STATUS_PASSEIO.EM_ANDAMENTO)}
              rotulo="Em andamento"
              para="/passeios"
            />
            <Indicador
              icone="check"
              tom="lilas"
              valor={contar(PAPEL.TUTOR, STATUS_PASSEIO.FINALIZADO)}
              rotulo="Concluídos"
              para="/passeios"
            />
            <Indicador
              icone="user"
              tom="marrom"
              valor={passeadores ? passeadores.length : null}
              rotulo="Passeadores disponíveis"
              para="/passeadores"
            />
          </div>
        </section>
      )}

      {ehPasseador && (
        <section className="painel-secao" aria-labelledby="titulo-passeador">
          <h2 id="titulo-passeador">{ehTutor ? 'Como passeador' : 'Resumo'}</h2>

          <div className="indicadores">
            <Indicador
              icone="send"
              tom="ambar"
              valor={contar(PAPEL.PASSEADOR, STATUS_PASSEIO.SOLICITADO)}
              rotulo="Para responder"
              para="/passeios"
            />
            <Indicador
              icone="calendar"
              tom="azul"
              valor={contar(PAPEL.PASSEADOR, STATUS_PASSEIO.ACEITO)}
              rotulo="Aceitos"
              para="/passeios"
            />
            <Indicador
              icone="play"
              tom="verde"
              valor={contar(PAPEL.PASSEADOR, STATUS_PASSEIO.EM_ANDAMENTO)}
              rotulo="Em andamento"
              para="/passeios"
            />
            <Indicador
              icone="check"
              tom="lilas"
              valor={contar(PAPEL.PASSEADOR, STATUS_PASSEIO.FINALIZADO)}
              rotulo="Concluídos"
              para="/passeios"
            />
          </div>
        </section>
      )}

      <div className="painel-colunas">
        <section className="painel-secao" aria-labelledby="titulo-acoes">
          <h2 id="titulo-acoes">Ações rápidas</h2>

          <div className="acoes-rapidas">
            {ehTutor && (
              <>
                <AcaoRapida
                  para="/pets/novo"
                  icone="plus"
                  titulo="Cadastrar pet"
                  texto="Adicione um novo integrante"
                />
                <AcaoRapida
                  para="/passeadores"
                  icone="search"
                  titulo="Buscar passeadores"
                  texto="Escolha quem vai passear"
                />
              </>
            )}
            <AcaoRapida
              para="/passeios"
              icone="calendar"
              titulo={ehTutor ? 'Meus passeios' : 'Solicitações e passeios'}
              texto="Acompanhe o status de cada um"
            />
            <AcaoRapida
              para="/notificacoes"
              icone="bell"
              titulo="Notificações"
              texto={naoLidas > 0 ? `${naoLidas} não lidas` : 'Tudo em dia'}
            />
          </div>
        </section>

        <section className="painel-secao" aria-labelledby="titulo-recentes">
          <h2 id="titulo-recentes">Passeios recentes</h2>

          {passeios === null && !erro && (
            <div className="skeleton" style={{ height: 180 }} />
          )}

          {passeios !== null && recentes.length === 0 && (
            <p className="painel-vazio">Seus passeios vão aparecer aqui.</p>
          )}

          {recentes.length > 0 && (
            <ul className="recentes">
              {recentes.map((passeio) => (
                <li key={passeio.id}>
                  <Link to="/passeios" className="recente">
                    <span className="recente-texto">
                      <strong>{passeio.nomePet}</strong>
                      <small>{FORMATO_DATA_HORA.format(new Date(passeio.dataHora))}</small>
                    </span>
                    <StatusBadge status={passeio.status} />
                  </Link>
                </li>
              ))}
            </ul>
          )}

          {notificacoes.length > 0 && (
            <Link className="painel-link" to="/notificacoes">
              Última notificação: {notificacoes[0].mensagem}
            </Link>
          )}
        </section>
      </div>
    </PageShell>
  );
};

export default HomePage;
