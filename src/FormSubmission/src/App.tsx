import { useEffect, useState } from 'react'
import './App.css'

interface FormField {
  type: 'text_input',
  props: {
    text_input?: TextInputDetails
  }
}

interface TextInputDetails {
  placeholder: string | null
}

function App() {

  const [fields, setFields] = useState<FormField[]>([])

  useEffect(() => {

    setFields([
      {
        type: 'text_input',
        props: {
          text_input: {
            placeholder: 'Enter your name'
          }
        }
      },
      {
        type: 'text_input',
        props: {
          text_input: {
            placeholder: 'Enter your email'
          }
        }
      }
    ])
  }, [])

  return (
    <form>

      {fields.map((field, index) => {
        switch (field.type) {
          case 'text_input':
            const { text_input } = field.props;
            if (!text_input) {
              return null
            }
            return (
              <div key={index}>
                <input
                  type='text'
                  placeholder={text_input.placeholder || ''}
                />
              </div>
            )
          default:
            return null
        }
      })}

      <button type='submit'>Submit</button>
    </form>
  )
}

export default App
