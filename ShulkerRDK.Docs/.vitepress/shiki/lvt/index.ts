import grammar from './grammar.json'

const g = grammar as any

function makeLang(name: string) {
  return {
    name,
    scopeName: g.scopeName,
    patterns: g.patterns,
    repository: g.repository,
    embeddedLangs: []
  }
}

export const lvt = [makeLang('lvt'), makeLang('levitate')]
