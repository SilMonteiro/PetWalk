import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import AuthLayout from '../components/AuthLayout';
import Icon from '../components/Icon';
import { cadastrar } from '../services/usuarioService';
import {
  ROTULO_TIPO_COBRANCA,
  ROTULO_TIPO_PERFIL,
  TIPO_COBRANCA,
  TIPO_PERFIL
} from '../constants/enums';

const DESCRICAO_PERFIL = {
  [TIPO_PERFIL.TUTOR]: 'Tenho pets e quero contratar passeios',
  [TIPO_PERFIL.PASSEADOR]: 'Quero oferecer passeios',
  [TIPO_PERFIL.AMBOS]: 'Quero contratar e oferecer passeios'
};

const ICONE_PERFIL = {
  [TIPO_PERFIL.TUTOR]: 'heart',
  [TIPO_PERFIL.PASSEADOR]: 'leash',
  [TIPO_PERFIL.AMBOS]: 'pata'
};

const CadastroPage = () => {
  const navigate = useNavigate();

  const [nome, setNome] = useState('');
  const [email, setEmail] = useState('');
  const [telefone, setTelefone] = useState('');
  const [endereco, setEndereco] = useState('');
  const [tipoPerfil, setTipoPerfil] = useState(TIPO_PERFIL.TUTOR);
  const [senha, setSenha] = useState('');
  const [valorHora, setValorHora] = useState('');
  const [tipoCobranca, setTipoCobranca] = useState(TIPO_COBRANCA.POR_HORA);

  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const [enviando, setEnviando] = useState(false);

  const perfilPasseador =
    tipoPerfil === TIPO_PERFIL.PASSEADOR || tipoPerfil === TIPO_PERFIL.AMBOS;

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (enviando) {
      return;
    }

    setErro('');
    setSucesso('');

    const dadosCadastro = {
      nome,
      email,
      telefone,
      endereco,
      tipoPerfil,
      senha
    };

    // Só o perfil que passeia envia cobrança; sem ela o passeador nasce com valor 0.
    if (perfilPasseador) {
      dadosCadastro.valorHora = Number(valorHora);
      dadosCadastro.tipoCobranca = tipoCobranca;
    }

    setEnviando(true);

    try {
      const resposta = await cadastrar(dadosCadastro);
      setSucesso(resposta.mensagem);

      setTimeout(() => {
        navigate('/login');
      }, 1500);
    } catch (falha) {
      setErro(falha.message);
      setEnviando(false);
    }
  };

  return (
    <AuthLayout
      titulo="Crie sua conta"
      subtitulo="Leva um minutinho e já dá para começar."
      rodape={
        <>
          Já tem conta? <Link to="/login">Entrar</Link>
        </>
      }
    >
      <form onSubmit={handleSubmit} className="auth-form">
        <fieldset className="perfis">
          <legend className="campo-rotulo">Eu sou</legend>

          <div className="perfis-grade">
            {Object.entries(ROTULO_TIPO_PERFIL).map(([valor, rotulo]) => {
              const numero = Number(valor);

              return (
                <label
                  key={valor}
                  className={`perfil-opcao${tipoPerfil === numero ? ' selecionado' : ''}`}
                >
                  <input
                    type="radio"
                    name="tipoPerfil"
                    value={numero}
                    checked={tipoPerfil === numero}
                    onChange={() => setTipoPerfil(numero)}
                  />
                  <Icon nome={ICONE_PERFIL[numero]} />
                  <strong>{rotulo}</strong>
                  <small>{DESCRICAO_PERFIL[numero]}</small>
                </label>
              );
            })}
          </div>
        </fieldset>

        <div className="campo">
          <label htmlFor="nome">Nome</label>
          <div className="input-com-icone">
            <Icon nome="user" />
            <input
              type="text"
              id="nome"
              className="input"
              value={nome}
              onChange={(e) => setNome(e.target.value)}
              autoComplete="name"
              required
            />
          </div>
        </div>

        <div className="linha-campos">
          <div className="campo">
            <label htmlFor="e-mail">E-mail</label>
            <div className="input-com-icone">
              <Icon nome="mail" />
              <input
                type="email"
                id="e-mail"
                className="input"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                autoComplete="email"
                required
              />
            </div>
          </div>

          <div className="campo">
            <label htmlFor="telefone">Telefone</label>
            <div className="input-com-icone">
              <Icon nome="phone" />
              <input
                type="tel"
                id="telefone"
                className="input"
                value={telefone}
                onChange={(e) => setTelefone(e.target.value)}
                autoComplete="tel"
                required
              />
            </div>
          </div>
        </div>

        <div className="campo">
          <label htmlFor="endereco">Endereço</label>
          <div className="input-com-icone">
            <Icon nome="pin" />
            <input
              type="text"
              id="endereco"
              className="input"
              value={endereco}
              onChange={(e) => setEndereco(e.target.value)}
              autoComplete="street-address"
              required
            />
          </div>
        </div>

        {perfilPasseador && (
          <div className="auth-bloco-passeador entra">
            <p className="auth-bloco-titulo">
              <Icon nome="wallet" />
              Como você cobra pelos passeios
            </p>

            <div className="linha-campos">
              <div className="campo">
                <label htmlFor="valorHora">Valor por hora (R$)</label>
                <input
                  type="number"
                  id="valorHora"
                  className="input"
                  min="0.01"
                  step="0.01"
                  value={valorHora}
                  onChange={(e) => setValorHora(e.target.value)}
                  required
                />
              </div>

              <div className="campo">
                <label htmlFor="tipoCobranca">Tipo de cobrança</label>
                <select
                  id="tipoCobranca"
                  className="select"
                  value={tipoCobranca}
                  onChange={(e) => setTipoCobranca(Number(e.target.value))}
                  required
                >
                  <option value={TIPO_COBRANCA.POR_HORA}>
                    {ROTULO_TIPO_COBRANCA[TIPO_COBRANCA.POR_HORA]}
                  </option>
                  <option value={TIPO_COBRANCA.POR_HORA_COM_ADICIONAL_DE_PORTE}>
                    {ROTULO_TIPO_COBRANCA[TIPO_COBRANCA.POR_HORA_COM_ADICIONAL_DE_PORTE]}
                  </option>
                </select>
              </div>
            </div>
          </div>
        )}

        <div className="campo">
          <label htmlFor="senha">Senha</label>
          <div className="input-com-icone">
            <Icon nome="lock" />
            <input
              type="password"
              id="senha"
              className="input"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              autoComplete="new-password"
              required
            />
          </div>
        </div>

        {erro && (
          <p className="alerta alerta--erro" role="alert">
            <Icon nome="alert" />
            {erro}
          </p>
        )}
        {sucesso && (
          <p className="alerta alerta--sucesso" role="status">
            <Icon nome="check" />
            {sucesso}
          </p>
        )}

        <button type="submit" className="btn btn--lg btn--block" disabled={enviando}>
          {enviando ? <span className="spinner" /> : null}
          {enviando ? 'Cadastrando...' : 'Cadastrar'}
        </button>
      </form>
    </AuthLayout>
  );
};

export default CadastroPage;
