import type { SubjectLensId } from './subject-lenses';

export type SubjectExplorerPrompt = {
  id: SubjectLensId;
  shortLabel: string;
  invitation: string;
  tryThis: string;
  showIt: string;
};

export const subjectExplorerPrompts: readonly SubjectExplorerPrompt[] = [
  { id: 'english', shortLabel: 'English', invitation: 'Read, talk and write to share meaning.', tryThis: 'Choose a short paragraph. Find one word or sentence that gives you a strong picture in your mind.', showIt: 'Point to the clue and explain what it helped you imagine.' },
  { id: 'mathematics', shortLabel: 'Maths', invitation: 'Spot patterns and explain why an answer makes sense.', tryThis: 'Choose twelve small objects. Arrange them in as many equal groups as you can.', showIt: 'Draw each arrangement and explain how you know the groups are equal.' },
  { id: 'science', shortLabel: 'Science', invitation: 'Observe carefully, ask questions and explain with evidence.', tryThis: 'Compare two safe everyday objects. Write three things you can observe and one question you cannot answer by looking.', showIt: 'Make a labelled drawing that separates what you observed from what you think might be true.' },
  { id: 'history', shortLabel: 'History', invitation: 'Use clues from the past to build a careful account.', tryThis: 'With an adult, choose an old photograph or safe object. Ask who made it, when it was used and what it cannot tell you.', showIt: 'Write one claim the source supports and one question that still needs another source.' },
  { id: 'languages', shortLabel: 'Languages', invitation: 'Listen, speak, read and write to communicate in another language.', tryThis: 'Ask a teacher for one checked phrase. Listen to it, match it to its meaning and use it in a tiny exchange.', showIt: 'Say or write the phrase in the right situation, then explain what it means.' },
  { id: 'computing', shortLabel: 'Computing', invitation: 'Break problems into steps, test them and improve them safely.', tryThis: 'Write exact instructions for moving a counter around a small paper grid. Ask someone to follow only your instructions.', showIt: 'Mark where the instructions worked, where they failed and the one change that fixed them.' },
  { id: 'geography-environment', shortLabel: 'Geography', invitation: 'Explore places, patterns and how people and environments connect.', tryThis: 'Draw a simple map of one familiar room or safe route. Add a key and show where important features are.', showIt: 'Explain one choice you made about scale, symbols or direction.' },
  { id: 'art-design', shortLabel: 'Art & design', invitation: 'Look closely, try ideas and make purposeful choices.', tryThis: 'Choose one ordinary object and sketch it three ways: outline, texture and shape.', showIt: 'Circle the sketch that best shows your idea and explain one choice you would keep.' },
  { id: 'music-performance', shortLabel: 'Music', invitation: 'Listen, create, rehearse and respond with sound or movement.', tryThis: 'Make a four-beat pattern using claps and taps. Repeat it, then change just one beat.', showIt: 'Perform both patterns and say exactly what changed.' },
  { id: 'physical-education', shortLabel: 'PE', invitation: 'Build safe movement, teamwork and thoughtful practice.', tryThis: 'With safe space and adult agreement, practise a short three-part movement sequence slowly.', showIt: 'Name one part you improved and the feedback or practice that helped.' },
  { id: 'citizenship-media', shortLabel: 'Media & citizenship', invitation: 'Check claims, consider people and take part responsibly.', tryThis: 'Choose a harmless claim from a teacher-provided source. Find who made it, when and what evidence they give.', showIt: 'Sort what you found into fact, opinion and something still uncertain.' },
  { id: 'health-wellbeing', shortLabel: 'Health & wellbeing', invitation: 'Learn safe, practical ways to care for yourself and others.', tryThis: 'Make a simple plan for a calm study break using movement, water and a clear return time.', showIt: 'Explain why each part is safe and when a trusted adult should help instead.' },
  { id: 'money-life-skills', shortLabel: 'Money & life', invitation: 'Use numbers and information to make careful everyday choices.', tryThis: 'Use pretend prices to plan a snack for a fixed pretend budget. Compare at least two choices.', showIt: 'Show the total, the money left and one reason for your choice. Never use real account details.' },
  { id: 'religion-philosophy', shortLabel: 'Belief & philosophy', invitation: 'Explore beliefs, reasons and meanings with care and respect.', tryThis: 'Use two teacher-checked descriptions of a belief or practice. Find one similarity and one difference.', showIt: 'Explain the comparison without asking anyone to reveal their personal belief.' },
] as const;
